#!/usr/bin/env python3
from json import dumps, loads
from random import randint
import datetime
import sys
import time

import darts.models as models
import darts
import pandas as pd

# model
model = models.ARIMA()

# get data from stdin, otherwise generate random data
def getData() -> dict[str, list[datetime.datetime] | list[int]]:
    if sys.stdin.isatty():
        now = datetime.datetime.now()
        return {
            "time": [
                now + datetime.timedelta(seconds=i) for i in range(100)
            ], 
            "value": [
                randint(0, 100) for _ in range(100)
            ]
        }
    else:
        return loads(sys.stdin.readline())

# main loop
trained = False
while True:
    prediction = {}
    historical = getData()
    if historical["time"]:
        trained = True
        # retrain model with incoming data
        dataframe = pd.DataFrame(historical)
        ts = darts.TimeSeries.from_dataframe(dataframe, 'time', 'value', freq='s')
        model.fit(ts)
        prediction = model.predict(10)
    elif not trained:
        print(dumps([]))
        sys.stderr.write("No data to train model with\n")
        continue
    else:
        prediction = model.predict(10)
    print(dumps([int(x) for x in prediction.values()]))
    time.sleep(int(sys.argv[-1]))
