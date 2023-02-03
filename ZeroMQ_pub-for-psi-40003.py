import zmq, random, datetime, json, time

context = zmq.Context()
socket = context.socket(zmq.PUB)
# socket.bind('tcp://127.0.0.1:40003')
socket.bind('tcp://*:40003')
# socket.bind('tcp://localhost:40003')
text = "Hi from ZeroMQ pub"

while True:
    print(f"Sending text: {text}")
    payload = {}
    payload['message'] = text
    payload['originatingTime'] = datetime.datetime.now().isoformat()
    socket.send_multipart(['Remote_PSI_Text'.encode(), json.dumps(payload).encode('utf-8')])
    time.sleep(2)
