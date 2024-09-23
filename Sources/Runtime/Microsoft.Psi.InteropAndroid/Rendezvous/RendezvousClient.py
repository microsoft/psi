# Copyright (c) Microsoft Corporation. All rights reserved.
# Licensed under the MIT license.

import socket, struct, threading
from enum import IntEnum

# Client which connects to a RendezvousServer and relays rendezvous information.
class RendezvousClient:
    PROTOCOL_VERSION = 2

    def __init__(self, host, port = 13331):
        self.serverAddress = (host, port)
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    def __sendByte(self, b):
        self.socket.send(struct.pack('b', b))

    def __readByte(self):
        b, = struct.unpack('b', self.socket.recv(1))
        return b

    def __sendInt(self, c):
        self.socket.send(struct.pack('<i', c))

    def __readInt(self):
        i, = struct.unpack('<i', self.socket.recv(4))
        return i

    def __sendString(self, s):
        e = s.encode()
        self.socket.send(struct.pack('b%ds' % len(e), len(e), e)) # assumes len < 128 (LEB128 encoded)
    
    def __readString(self):
        len = self.__readByte()
        buf = self.socket.recv(len)
        str, = struct.unpack('%ds' % len, buf)
        return str

    def __sendProtocolVersion(self):
        self.socket.send(struct.pack('<h', self.PROTOCOL_VERSION)) # protocol version

    def __readProtocolVersion(self):
        version, = struct.unpack('<h', self.socket.recv(2))
        if version != self.PROTOCOL_VERSION:
            raise Exception('RendezvousClient protocol mismatch %d' % self.PROTOCOL_VERSION);

    class Endpoint(IntEnum):
        TcpSource = 0
        NetMQSource = 1
        RemoteExporter = 2
        RemoteClockExporter = 3

    class TransportKind(IntEnum):
        Tcp = 0
        Udp = 1
        NamedPipes = 2

    def createStream(name, type):
        return { 'name': name,
                 'type': type }

    def createTcpEndpoint(host, port, streams):
        return { 'endpoint': RendezvousClient.Endpoint.TcpSource,
                 'host': host,
                 'port': port,
                 'streams': streams }

    def createNetMQEndpoint(address, streams):
        return { 'endpoint': RendezvousClient.Endpoint.NetMQSource,
                 'address': address,
                 'streams': streams }

    def createRemoteExporterEndpoint(host, port, transport, streams):
        return { 'endpoint': RendezvousClient.Endpoint.RemoteExporter,
                 'host': host,
                 'port': port,
                 'transport': transport,
                 'streams': streams }

    def createRemoteClockExporterEndpoint(host, port):
        return { 'endpoint': RendezvousClient.Endpoint.RemoteClockExporter,
                 'host': host,
                 'port': port,
                 'streams': [] }

    def createProcess(name, endpoints, version):
        return { 'name': name,
                 'endpoints': endpoints,
                 'version': version }

    def start(self, processAddedCallback = None, processRemovedCallback = None):
        self.socket.connect(self.serverAddress)
        self.onProcessAdded = processAddedCallback
        self.onProcessRemoved = processRemovedCallback
        self.__sendProtocolVersion()
        self.__readProtocolVersion()
        buf = self.socket.recv(1)
        len = struct.unpack('B')
        buf = self.socket.recv(len)
        addr, = struct.unpack('%ds' % len, buf)
        self.clientAddress = addr
        buf = self.socket.recv(4)
        numProcesses, = struct.unpack('<i', buf)
        for _ in range(numProcesses):
            self.__readProcessUpdate()
        self.thread = threading.Thread(target=self.__readProcessUpdates)
        self.thread.start()

    def stop(self):
        self.__sendByte(0) # disconnect
        self.socket.close()

    def addProcess(self, process):
        self.__sendByte(1) # add process
        self.__sendString(process['name'])
        self.__sendString(process['version'])
        self.__sendInt(len(process['endpoints']))
        for e in process['endpoints']:
            self.__sendByte(e['endpoint'])
            if e['endpoint'] == RendezvousClient.Endpoint.TcpSource:
                self.__sendString(e['host'])
                self.__sendInt(e['port'])
            elif e['endpoint'] == RendezvousClient.Endpoint.NetMQSource:
                self.__sendString(e['address'])
            elif e['endpoint'] == RendezvousClient.Endpoint.RemoteExporter:
                self.__sendString(e['host'])
                self.__sendInt(e['port'])
                self.__sendInt(e['transport'])
            elif e['endpoint'] == RendezvousClient.Endpoint.RemoteClockExporter:
                self.__sendString(e['host'])
                self.__sendInt(e['port'])
            else:
                raise Exception('Unknown type of Endpoint.')
            self.__sendInt(len(e['streams']))
            for s in e['streams']:
                self.__sendString(s['name'])
                self.__sendString(s['type'])

    def removeProcess(self, process):
        self.__sendByte(2) # remove process
        self.__sendString(process['name'])

    def __readStreams(self):
        streams = []
        count = self.__readInt()
        for _ in range(count):
            name = self.__readString()
            type = self.__readString()
            streams.append(RendezvousClient.createStream(name, type))
        return streams

    def __readEndpoint(self):
        endpoint = RendezvousClient.Endpoint(self.__readByte())
        if endpoint == RendezvousClient.Endpoint.TcpSource:
            host = self.__readString()
            port = self.__readInt()
            streams = self.__readStreams()
            return RendezvousClient.createTcpEndpoint(host, port, streams)
        elif endpoint == RendezvousClient.Endpoint.NetMQSource:
            address = self.__readString()
            streams = self.__readStreams()
            return RendezvousClient.createNetMQEndpoint(address, streams)
        elif endpoint == RendezvousClient.Endpoint.RemoteExporter:
            host = self.__readString()
            port = self.__readInt()
            transport = RendezvousClient.TransportKind(self.__readInt())
            streams = self.__readStreams()
            return RendezvousClient.createRemoteExporterEndpoint(host, port, transport, streams)
        elif endpoint == RendezvousClient.Endpoint.RemoteClockExporter:
            host = self.__readString()
            port = self.__readInt()
            self.__readInt() # stream count (zero)
            return RendezvousClient.createRemoteClockExporterEndpoint(host, port)
        else:
            raise Exception("Unknown type of Endpoint.")

    def __readProcess(self):
        name = self.__readString()
        version = self.__readString()
        numEndpoints = self.__readInt()
        endpoints = []
        for _ in range(numEndpoints):
            endpoints.append(self.__readEndpoint())
        return RendezvousClient.createProcess(name, endpoints, version)

    rendezvous = {}

    def __readProcessUpdate(self):
        update = self.__readByte()
        if update == 0: # disconnect
            return False
        elif update == 1: # add process
            process = self.__readProcess()
            self.rendezvous[process['name']] = process
            if self.onProcessAdded is not None:
                self.onProcessAdded(process)
            return True
        elif update == 2: # remove process
            name = self.__readString()
            removed = self.rendezvous.pop(name)
            if self.onProcessRemoved is not None:
                self.onProcessRemoved(removed)
            return True
        else:
            raise Exception("Unexpected rendezvous action.")

    def __readProcessUpdates(self):
        try:
            while self.__readProcessUpdate():
                pass
        except: # socket closed?
            pass