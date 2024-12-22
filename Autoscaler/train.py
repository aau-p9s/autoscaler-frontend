#!/usr/bin/env python3
import darts.models as models
import darts
import pandas as pd

from utils import getData, format_prediction

# main loop
dataframe = pd.DataFrame(getData())
ts = darts.TimeSeries.from_dataframe(dataframe, "time", "value", freq='min')
# model
model = models.StatsForecastAutoTheta(season_length=120)
model.fit(ts)
model.save("model/autotheta_model.pth")