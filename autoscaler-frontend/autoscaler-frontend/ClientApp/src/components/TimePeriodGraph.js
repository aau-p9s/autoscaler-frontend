import React, { useState, useEffect, useRef } from 'react';
import { Line } from 'react-chartjs-2';
import 'chart.js/auto';
import Chart from 'chart.js/auto';
import dragDataPlugin from 'chartjs-plugin-dragdata';

// Register the plugin
Chart.register(dragDataPlugin);

const TimePeriodGraph = () => {
    const [timePeriod, setTimePeriod] = useState('hour');
    const [chartData, setChartData] = useState(null); // Start with null since data is fetched
    const [isLoading, setIsLoading] = useState(true); // Track loading state
    const chartRef = useRef(null);
  
    const generateData = async (interval) => {
        let labels;
        let data;

        if (interval === 'hour') {
            labels = Array.from({ length: 12 }, (_, i) => `${i * 5} min`);
            data = [45, 50, 55, 60, 58, 63, 70, 68, 75, 80, 85, 90];
        } else if (interval === 'day') {
            labels = Array.from({ length: 24 }, (_, i) => `${i}:00`);
            data = [30, 35, 28, 40, 45, 50, 55, 60, 62, 65, 70, 72, 75, 78, 80, 82, 85, 87, 90, 92, 95, 98, 100, 105];
        } else if (interval === 'week') {
            labels = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
            var response = await (fetch("http://localhost:5280/forecast"))
            var json = await response.json()
            data = Object.keys(json.item2).map(key => json.item2[key])
        }

        return  {
            labels,
            datasets: [
                {
                    label: `Forecast for the next ${timePeriod}`,
                    data,
                    fill: false,
                    backgroundColor: 'rgba(75, 192, 192, 0.6)',
                    borderColor: 'rgba(75, 192, 192, 1)',
                },
            ],
        };
    };

    useEffect(() => {
        const fetchData = async () => {
          setIsLoading(true);
          const data = await generateData(timePeriod);
          setChartData(data);
          setIsLoading(false);
        };
    
        fetchData();
      }, [timePeriod]);
    
      if (isLoading) {
        return <div>Loading chart data...</div>;
    }

    const options = {
        responsive: true,
        maintainAspectRatio: false,
        scales: {
            x: {
                title: {
                    display: true,
                    text: 'Time',
                },
            },
            y: {
                title: {
                    display: true,
                    text: 'Value',
                },
            },
        },
        plugins: {
            dragData: {
                onDrag: (e, datasetIndex, index, value) => {
                    const updatedData = [...chartData.datasets[datasetIndex].data];
                    updatedData[index] = value;
                    setChartData((prev) => ({
                        ...prev,
                        datasets: [
                            {
                                ...prev.datasets[datasetIndex],
                                data: updatedData,
                            },
                        ],
                    }));
                },
                magnet: {
                    to: Math.round
                },
                onDragEnd: () => {
                    console.log('Drag ended, data saved.');
                },
            },
            legend: {
                display: true,
            },
        },
    };

    const handleTimePeriodChange = (period) => {
        setTimePeriod(period);
        setChartData(generateData(period));
    };
    const handleSave = () => {
        console.log('Current chart data:', chartData);
        // localStorage.setItem('chartData', JSON.stringify(chartData));
        alert('Data has been saved!');
    };

    return (
        <div style={{ width: '80%', height: '400px', margin: '0 auto' }}>
            <Line ref={chartRef} data={chartData} options={options} />
            <div style={{ marginBottom: '20px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                <div>
                <button onClick={() => handleTimePeriodChange('hour')} style={buttonStyle}>Next Hour</button>
                <button onClick={() => handleTimePeriodChange('day')} style={buttonStyle}>Next Day</button>
                <button onClick={() => handleTimePeriodChange('week')} style={buttonStyle}>Next Week</button>
                </div>
                <button onClick={handleSave} style={buttonStyle}>Save Data</button>
            </div>
        </div>
    );
};

const buttonStyle = {
    backgroundColor: 'rgba(75, 192, 192, 0.6)',
    borderColor: 'rgba(255,255,255)',
    borderRadius: '5px',
    color: '#000',
    padding: '10px 20px',
    margin: '0 10px',
    cursor: 'pointer',
    fontSize: '16px',
    transition: 'all 0.3s ease',
};

export default TimePeriodGraph;
