import zmq, json, time

socket = zmq.Context().socket(zmq.SUB)
socket.connect("tcp://128.2.204.249:40002")
socket.setsockopt(zmq.SUBSCRIBE, b'') # '' means all topics
# socket.setsockopt_string(zmq.SUBSCRIBE, '') # '' means all topics
# socket.setsockopt(zmq.SUBSCRIBE, 'PSI_Remote_Text')


while True:
    [topic, message] = socket.recv_multipart()
    j = json.loads(message)
    print ("ZeroMQ_sub received: ", repr(j['message']))
    # print (f"ZeroMQ_sub received: ", repr(j['message']))
    # print(f"Sending text: {text}")
    # print "Originating Time: ", repr(j['originatingTime'])
    # time.sleep(1)