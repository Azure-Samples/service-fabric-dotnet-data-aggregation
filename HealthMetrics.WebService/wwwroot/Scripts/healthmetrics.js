
var api = new Api();

function MetricsApp() {
    var self = this;
    this.currentContext = 'user';
    this.currentBandId = '';
    this.currentDoctorId = '';
    this.currentDoctorName = '';
    this.currentPersonName = '';
    this.countUpOptions = {
        useEasing: false
    };

    this.RandomElement = function (arr) {
        return arr[Math.floor(Math.random() * arr.length)];
    };

    this.FormatDateTime = function (dateObject) {
        var d = new Date(dateObject);
        return d.getTime();
    };

    this.Initialize = function () {

        if (window.location.hash == "#doctors") {
            self.SetDoctorContext();
        }
        else {
            self.SetUserContext();
        }

        setInterval(self.UpdateStats, 3000);
    }

    this.InitializeUsers = function () {

        api.GetIds(function (result) {
            var strings = result.split("|");
            self.currentBandId = strings[0];
            self.currentDoctorId = strings[1];
        });

        setInterval(self.UpdateUserInfo, 2000);
        //setInterval(self.UpdateDoctorInfo, 5000);
    }


    this.UpdateStats = function () {

        api.GetNationalStats(function (statsData) {

            var bandStats = $('#bandsStats');
            var doctorStats = $('#doctorsStats');
            var healthReportStats = $('#healthReportsStats');
            var messageRate = $('#messageRate')

            var creationDate = new Date(statsData.StartTimeOffset);
            var now = new Date();

            var timeInSeconds = (now - creationDate) / 1000;
            var rawCreationRate = statsData.HealthReportCount / timeInSeconds;
            var creationRate = Math.round(rawCreationRate * 100) / 100;

            bandStats.text(statsData.PatientCount);
            doctorStats.text(statsData.DoctorCount);
            healthReportStats.text(statsData.HealthReportCount);
            messageRate.text(creationRate);
        });
    }

    this.UpdateDoctorList = function (countyId) {
        api.GetDoctors(countyId, function (doctorListData) {
            var doctorList = $('#doctorListStats ul');
            doctorList.empty();

            $.each(doctorListData, function (id, jObject) {

                var healthClass = getHealthBoxClass(jObject.HealthStatus);
                var healthValue = getHealthBoxTextValue(jObject.HealthStatus);

                $('<li class="name-and-healthbox clearfix"/>')
                    .append(
                        $('<div/>').addClass(healthClass).css('background-color', computeColor(jObject.HealthStatus)).html(healthValue))
                    .append(
                        $('<span/>').text(jObject.DoctorName))
                    .appendTo(doctorList);
            });

            $('#doctorListStats .loadingMessage').hide();
        });
    }

    this.UpdateUserInfo = function () {
        api.GetPatient(self.currentBandId, function (patientData) {
            self.currentPersonName = patientData.PersonName;

            var healthBoxClass = getHealthBoxClass(patientData.HealthIndexValue);
            var healthBoxText = getHealthBoxTextValue(patientData.HealthIndexValue);

            var userHealthIndex = $('#userInfo-userHealthIndex');
            userHealthIndex.empty();
            userHealthIndex.append($('<div/>').addClass(healthBoxClass).css('background-color', computeColor(patientData.HealthIndexValue)).html(healthBoxText));
            userHealthIndex.append($('<span/>').text('Your Stress Index'));

            var heartRateTable = $('#userStats #heartRateTable');
            heartRateTable.empty();

            api.GetCountyHealth(patientData.CountyInfo.CountyId, function (value) {

                var healthBoxClass = getHealthBoxClass(value);
                var healthBoxText = getHealthBoxTextValue(value);

                var countyHealthIndex = $('#userInfo-countyHealthIndex');
                countyHealthIndex.empty();
                countyHealthIndex.append($('<div/>').addClass(healthBoxClass).css('background-color', computeColor(value)).html(healthBoxText));
                countyHealthIndex.append($('<span/>').text(patientData.CountyInfo.CountyName + ' average'));
            });

            var bptickpoints = [];

            var bpTickCount = 1;

            $.each(patientData.HeartRateHistory, function (id, jObject) {
                var tick = [];
                tick.push(self.FormatDateTime(jObject.Timestamp));
                tick.push(jObject.HeartRate);
                bpTickCount++;
                bptickpoints.push(tick);
            });

            DrawLineGraph("heartRateTable", "Resting Heart Rate", "", "", [bptickpoints], 50, 220);

            $('#userStats .loadingMessage').hide();

            if (self.currentContext == 'user') {
                self.SetUserName(self.currentPersonName);
            }

            self.UpdateDoctorList(patientData.CountyInfo.CountyId);
        });
    }

    this.SetUserContext = function () {
        this.SetUserName(this.currentPersonName);
        this.currentContext = 'user';
        $('.header-title h1').text('Health Metrics');
        $('.header-title h1').css('color', '#FF8A00');
        $('#doctorInfo').hide();
        $('#userInfo').show();
    }

    this.SetDoctorContext = function () {
        this.SetUserName(this.currentDoctorName);
        this.currentContext = 'doctor';
        $('.header-title h1').text('Health Metrics for Doctors');
        $('.header-title h1').css('color', '#00ABEC');
        $('#userInfo').hide();
        $('#doctorInfo').show();
    }

    this.GenerateColorLegend = function () {
        var colorLegend = $('#colorLegend');
        colorLegend.empty();
        for (var i = 1; i <= 100; ++i) {
            $('<div style="background-color: hsl(' + i + ', 100%, 50%); width:5px; height:20px; display:inline-block" />')
                .appendTo(colorLegend);
        }
    }


    this.SetUserName = function (name) {
        $('.login-user h3').text(name);
    };
}

$(function () {
    var metricsApp = new MetricsApp();
    metricsApp.Initialize();

    metricsApp.InitializeUsers();

    $('#userLoginLink').click(function () {
        metricsApp.SetUserContext();
    });

    $('#doctorLoginLink').click(function () {
        metricsApp.SetDoctorContext();
    });

});

var mapApp = {
    initialize: function () {
        var self = this;
        var scope = angular.element(document.getElementById('map')).scope();
        this.initializeMap();
        setInterval(scope.refreshCountyHealth, 5000);

        var width = ($(window).width() * .95) - 350;
        var height = width * .66;

        $("#usMap")
            .attr("width", width)
            .attr("height", height);

        $("#mapContainer")
            .css("margin-top", Math.round((width / 10) * -1))
            .css("float", "left");

        $("#map").on("mapRefresh", function (e, newValue) {
            e.stopPropagation();
            e.preventDefault();
            self.refreshMap(newValue.newData);
        })
    },
    initializeMap: function () {
        var self = this;
        var svg = d3.select("#usMap");

        var width = ($(window).width()) - 350;
        var height = width * .66;

        var projection = d3.geoAlbersUsa()
            .scale(width)
            .translate([width / 2, height / 2]);

        var path = d3.geoPath()
            .projection(projection);

        d3.json("/Content/us-10m.json", function (error, topology) {
            svg.selectAll(".region")
            .data(topojson.feature(topology, topology.objects.counties).features)
            .enter()
            .append("path")
            .attr("d", path)
            .attr('fill', function (d) { return '#313131'; })
            .attr("id", function (d) { return "p" + d.id; });
        });
    },
    refreshMap: function (newData) {
        var self = this;
        $.map(newData, function (data) {
            d3.select("path#p" + data.Id)
            .transition()
            .duration(1000)
            .attr('fill', function (d) {
                return computeColor(data.healthIndex);
            });
        });

    },
};

var resizeListener = function () {
    $(window).one("resize", function () {
        window.location.reload(true);
        setTimeout(resizeListener, 200); //rebinds itself after 200ms
    });
}

resizeListener();

function computeColor(HealthIndex) {
    if (HealthIndex.mode == false) {
        return 'hsl(0, 0%, ' + HealthIndex.value + '%)';
    }
    else {
        return 'hsl('+ HealthIndex.value +', 100%, 50%)';
    }
}

function countyHealthViewModel() {
    this.Id = 0;
    this.healthIndex = 0;
}

function getHealthBoxClass(HealthIndex) {
    if (HealthIndex.value == -1) {
        return 'healthBox-noinfo';
    }
    else {
        return 'healthBox';
    }
}

function getHealthBoxTextValue(HealthIndex) {
    if (HealthIndex.value == -1) {
        return 'na'
    }
    else {
        return HealthIndex.value;
    }
}

function DrawLineGraph(elementById, title, xAxisName, yAxisName, dataArray, yHeight, xWidth) {
    var table = $.jqplot(elementById, dataArray, {
        title: title,
        axesDefaults: {
            labelRenderer: $.jqplot.CanvasAxisLabelRenderer,
            tickRenderer: $.jqplot.CanvasAxisTickRenderer
        },
        seriesDefaults: {
            lineWidth: 1,
            markerOptions: { size: 4 },
            rendererOptions: {
                smooth: true
            }
        },
        gridDimensions: {
            height: yHeight,
            width: xWidth
        },
        axes: {
            xaxis: {
                renderer: $.jqplot.DateAxisRenderer,
                tickOptions: {
                    formatString: '%H:%M:%S',
                    textColor: '#FFF',
                    angle: -90
                },
                pad: 0
            },
            yaxis: {
                label: yAxisName,
                pad: 0,
                tickOptions: {
                    textColor: '#FFF',
                },
            }
        }
    });
}

function getCountyHealthViewModel(countyHealthData) {
    var ret = new countyHealthViewModel();

    ret.Id = countyHealthData.Id;
    ret.healthIndex = countyHealthData.Health;

    return ret;
}

angular.module('healthApp', [])
    .factory('healthService', function ($http) {
        return {
            listCountyHealth: function () {
                return $http.get('/api/national/health?' + (Math.random()));
            }
        };
    })
    .controller('homeController', function ($scope, healthService) {
        $scope.countyData = [];
        $scope.init = function () {

        };
        $scope.refreshCountyHealth = function () {
            healthService.listCountyHealth()
                .then(function (result) {
                    $scope.countyData = $.map(result.data, function (countyHealthData) {
                        return getCountyHealthViewModel(countyHealthData);
                    });
                });
        };
    })
    .directive('d3Host', function () {
        return {
            restrict: 'E',
            scope: {
                val: '='
            },
            link: function (scope, element, attrs) {
                scope.$watch('val', function (newValue, oldValue) {
                    if (newValue)
                        $("#map").trigger("mapRefresh", { newData: newValue });
                }, true);
            }
        };
    })
