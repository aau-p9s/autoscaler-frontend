#!/usr/bin/env python3
import darts.models as models
import sys

from utils import getData, format_prediction

model = models.ARIMA.load("model/model.pth")
prediction = model.predict(10)

print(format_prediction(prediction))
sys.stderr.write(format_prediction(prediction) + "\n")
