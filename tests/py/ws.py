import json
from websocket import create_connection

ws = create_connection("ws://127.0.0.1:8082/rpc")

req=json.dumps({"id":"ASzXY80R","method":"signin","params":[{"user":"root","pass":"root"}]})
ws.send(req)

rsp = ws.recv()
print(json.loads(rsp))

ws.close()
