import json
from random import randint, random
from websocket import create_connection

ws = create_connection('ws://127.0.0.1:8082/rpc')

req=json.dumps({'id':'ASzXY80R','method':'signin','params':[
  {'user':'root','pass':'root'}
]})
ws.send(req)
rsp = ws.recv()
print(json.loads(rsp))

req=json.dumps({'id':'ASzXY80R','method':'use','params':['test', 'test']})
ws.send(req)
rsp = ws.recv()
print(json.loads(rsp))

req=json.dumps({'id':'ASzXY80R','method':'create','params':[
    'person',
    {
      'title': 'Founder & CEO',
      'name': {
        'first': 'Tobie',
        'last': 'Morgan Hitchcock',
      },
      'marketing': True,
      'identifier': randint(0, 1000000),
    }
]})
ws.send(req)
rsp = ws.recv()
print(json.loads(rsp))

ws.close()
