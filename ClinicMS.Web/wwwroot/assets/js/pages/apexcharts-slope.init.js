

// get colors array from the string
function getChartColorsArray(chartId) {
    const chartElement = document.getElementById(chartId);
    if (chartElement) {
        const colors = chartElement.dataset.colors;
        if (colors) {
            const parsedColors = JSON.parse(colors);
            const mappedColors = parsedColors.map((value) => {
                const newValue = value.replace(/\s/g, "");
                if (!newValue.includes(",")) {
                    const color = getComputedStyle(document.documentElement).getPropertyValue(newValue);
                    return color || newValue;
                } else {
                    const val = value.split(",");
                    if (val.length === 2) {
                        const rgbaColor = `rgba(${getComputedStyle(document.documentElement).getPropertyValue(val[0])}, ${val[1]})`;
                        return rgbaColor;
                    } else {
                        return newValue;
                    }
                }
            });
            return mappedColors;
        } else {
            console.warn(`data-colors attribute not found on: ${chartId}`);
        }
    }
}

var basicSlopeChart = "";
var chartMultiChart = "";

function loadCharts() {
    // basic Chart
    var basicSlopeColors = "";
    basicSlopeColors = getChartColorsArray("basic_slope_chart");
    if (basicSlopeColors) {
        var options = {
            series: [
                {
                    name: 'Blue',
                    data: [
                        {
                            x: 'Jan',
                            y: 43,
                        },
                        {
                            x: 'Feb',
                            y: 58,
                        },
                    ],
                },
                {
                    name: 'Green',
                    data: [
                        {
                            x: 'Jan',
                            y: 33,
                        },
                        {
                            x: 'Feb',
                            y: 38,
                        },
                    ],
                },
                {
                    name: 'Red',
                    data: [
                        {
                            x: 'Jan',
                            y: 55,
                        },
                        {
                            x: 'Feb',
                            y: 21,
                        },
                    ],
                },
            ],
            chart: {
                height: 350,
                width: 400,
                type: 'line',
            },
            plotOptions: {
                line: {
                    isSlopeChart: true,
                },
            },
            colors: basicSlopeColors
        };

        if (basicSlopeChart != "")
            basicSlopeChart.destroy();
        basicSlopeChart = new ApexCharts(document.querySelector("#basic_slope_chart"), options);
        basicSlopeChart.render();
    }

    // Multi group Charts
    var chartMultiColors = "";
    chartMultiColors = getChartColorsArray("multi_group_chart");
    if (chartMultiColors) {
        var options = {
            series: [
                {
                    name: 'Blue',
                    data: [
                        {
                            x: 'Category 1',
                            y: 503,
                        },
                        {
                            x: 'Category 2',
                            y: 580,
                        },
                        {
                            x: 'Category 3',
                            y: 135,
                        },
                    ],
                },
                {
                    name: 'Green',
                    data: [
                        {
                            x: 'Category 1',
                            y: 733,
                        },
                        {
                            x: 'Category 2',
                            y: 385,
                        },
                        {
                            x: 'Category 3',
                            y: 715,
                        },
                    ],
                },
                {
                    name: 'Orange',
                    data: [
                        {
                            x: 'Category 1',
                            y: 255,
                        },
                        {
                            x: 'Category 2',
                            y: 211,
                        },
                        {
                            x: 'Category 3',
                            y: 441,
                        },
                    ],
                },
                {
                    name: 'Yellow',
                    data: [
                        {
                            x: 'Category 1',
                            y: 428,
                        },
                        {
                            x: 'Category 2',
                            y: 749,
                        },
                        {
                            x: 'Category 3',
                            y: 559,
                        },
                    ],
                },
            ],
            chart: {
                height: 350,
                width: 600,
                type: 'line',
            },
            plotOptions: {
                line: {
                    isSlopeChart: true,
                },
            },
            tooltip: {
                followCursor: true,
                intersect: false,
                shared: true,
            },
            dataLabels: {
                background: {
                    enabled: true,
                },
                formatter(val, opts) {
                    const seriesName = opts.w.config.series[opts.seriesIndex].name
                    return val !== null ? seriesName : ''
                },
            },
            yaxis: {
                show: true,
                labels: {
                    show: true,
                },
            },
            xaxis: {
                position: 'bottom',
            },
            legend: {
                show: true,
                position: 'top',
                horizontalAlign: 'left',
            },
            stroke: {
                width: [2, 3, 4, 2],
                dashArray: [0, 0, 5, 2],
                curve: 'smooth',
            },
            colors: chartMultiColors
        };

        if (chartMultiChart != "")
            chartMultiChart.destroy();
        chartMultiChart = new ApexCharts(document.querySelector("#multi_group_chart"), options);
        chartMultiChart.render();
    }
}
window.addEventListener("resize", function () {
    setTimeout(() => {
        loadCharts();
    }, 250);
});
loadCharts();