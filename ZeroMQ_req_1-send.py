import zmq, datetime, time, json

context = zmq.Context()
socket = context.socket(zmq.REQ)

print("Connecting to server...")
socket.connect("tcp://128.2.204.249:40001")   # bree
time.sleep(1)

# request = "tcp://72.95.139.140:40003"   # 140 W. Swissvale Ave.
request = "tcp://128.2.220.118:40003"     # erebor
# request = "tcp://128.2.149.108:40003"
# request = "tcp://23.227.148.141:40003"

# Send the request
payload = {}
payload['message'] = request
payload['originatingTime'] = datetime.datetime.utcnow().isoformat()
print(f"Sending request: {request}")
socket.send_string(request)

#  Get the reply
reply = socket.recv()
print(f"Received reply: {reply}")
