import React, { useState } from 'react';
import { Line } from 'react-chartjs-2';
import 'chart.js/auto';

const TimePeriodGraph = () => {
    const [timePeriod, setTimePeriod] = useState('hour');
    const generateData = (interval) => {
        let labels;
        let data;

        if (interval === 'hour') {
            labels = Array.from({ length: 12 }, (_, i) => `${i * 5} min`); // 5-minute intervals
            data = [45, 50, 55, 60, 58, 63, 70, 68, 75, 80, 85, 90]; // Dummy data for the next hour
        } else if (interval === 'day') {
            labels = Array.from({ length: 24 }, (_, i) => `${i}:00`); // Hourly intervals
            data = [30, 35, 28, 40, 45, 50, 55, 60, 62, 65, 70, 72, 75, 78, 80, 82, 85, 87, 90, 92, 95, 98, 100, 105]; // Dummy data for the next day
        } else if (interval === 'week') {
            labels = ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];
            data = [100, 110, 120, 130, 125, 135, 140]; // Dummy data for the next week
        }

        return { labels, data };
    };

    const { labels, data } = generateData(timePeriod);

    const chartData = {
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
                    text: 'Pods',
                },
            },
        },
    };

    return (
        <div style={{ width: '80%', height: '400px', margin: '0 auto'}}>
            <Line data={chartData} options={options} />
            <div style={{ marginBottom: '20px', display: 'flex', justifyContent:'center', alignItems:'center' }}>
                <button onClick={() => setTimePeriod('hour')} style={buttonStyle}>Next Hour</button>
                <button onClick={() => setTimePeriod('day')} style={buttonStyle}>Next Day</button>
                <button onClick={() => setTimePeriod('week')} style={buttonStyle}>Next Week</button>
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
