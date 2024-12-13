#!/usr/bin/env python3
from time import sleep
from json import dumps, loads
from random import randint
import datetime
import sys

historic = loads(sys.stdin.readline())
sys.stderr.write(f"{historic}\n")
# TODO: Machine learning
# End Machine learning

forecast = [{
    "time": datetime.datetime.now() + datetime.timedelta(seconds=i*10),
    "value": randint(0, 10)
} for i in range(7)]

print(dumps(forecast, default=str))
