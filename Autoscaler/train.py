#!/usr/bin/env python3
import darts.models as models
import darts
import pandas as pd

from utils import getData, format_prediction

# main loop
dataframe = pd.DataFrame(getData())
ts = darts.TimeSeries.from_dataframe(dataframe, "time", "value", freq='s')
# model
model = models.ARIMA()
model.fit(ts)
model.save("model/model.pth")
prediction = model.predict(10)
print(format_prediction(prediction))
