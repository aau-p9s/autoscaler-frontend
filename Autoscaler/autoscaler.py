#!/usr/bin/env python3
from time import sleep
from json import dumps
from random import randint
import datetime

print(dumps([{
    "time": (datetime.date.today() + datetime.timedelta(days=i+1)),
    "value": randint(0, 10)
} for i in range(7)], default=str))
