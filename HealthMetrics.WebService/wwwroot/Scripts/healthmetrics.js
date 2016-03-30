
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

        //var year = d.getFullYear();
        //var month = d.getMonth();

        //if (month.toString().length == 1) {
        //    month = "0" + month.toString();
        //}

        //var day = d.getDay();

        //if (day.toString().length == 1) {
        //    day = "0" + day.toString();
        //}

        //var hour = d.getHours();

        //if (hour.toString().length == 1) {
        //    hour = "0" + hour.toString();
        //}

        //var min = d.getMinutes();

        //if (min.toString().length == 1) {
        //    min = "0" + min.toString();
        //}

        //var sec = d.getSeconds();

        //if (sec.toString().length == 1) {
        //    sec = "0" + sec.toString();
        //}

        //var time = year + "-" + month + "-" + day + " " + hour + ":" + min + ":" + sec;

        //return time;
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

    this.InitializeKnownUsers = function () {
        api.GetSetting("KnownPatientId", function (resultValue) {
            self.currentBandId = resultValue;
        });

        api.GetSetting("KnownDoctorId", function (resultValue) {
            self.currentDoctorId = resultValue;
        });

        setInterval(self.UpdateUserInfo, 2000);
        setInterval(self.UpdateDoctorInfo, 5000);
    }

    this.InitializeRandomUsers = function () {
        api.GetNationalHealth(function (countyInitData) {
            if (countyInitData.length < 2) {
                setTimeout(self.InitializeRandomUsers, 3000);
                return;
            }

            var userViewDoctor = self.RandomElement(countyInitData);
            var doctorViewDoctor = self.RandomElement(countyInitData);

            api.GetDoctors(userViewDoctor.CountyId, function (doctorListInitData) {
                var doctor = self.RandomElement(doctorListInitData);

                api.GetDoctor(doctor.DoctorId, function (doctorInitData) {
                    self.currentBandId = self.RandomElement(doctorInitData.Patients).PatientId;
                    self.UpdateUserInfo();
                    setInterval(self.UpdateUserInfo, 2000);
                });
            });

            api.GetDoctors(doctorViewDoctor.CountyId, function (doctorListInitData) {
                self.currentDoctorId = self.RandomElement(doctorListInitData).DoctorId;
                self.UpdateDoctorInfo();
                setInterval(self.UpdateDoctorInfo, 5000);
            });
        });
    };

    this.UpdateStats = function () {

        api.GetNationalStats(function (statsData) {

            var bandStats = $('#bandsStats');
            var doctorStats = $('#doctorsStats');
            var healthReportStats = $('#healthReportsStats');

            bandStats.text(statsData.PatientCount);
            doctorStats.text(statsData.DoctorCount);
            healthReportStats.text(statsData.HealthReportCount);
        });
    }

    this.UpdateDoctorInfo = function () {
        api.GetDoctor(self.currentDoctorId, function (doctorData) {
            self.currentDoctorName = doctorData.DoctorName;
            var doctorInfo = $('#doctorInfo');

            var patientHealthIndex = $('#doctorInfo-patientHealthIndex');
            patientHealthIndex.empty();

            var healthBoxClass = getHealthBoxClass(doctorData.AveragePatientHealthIndex);
            var healthBoxValue = getHealthBoxTextValue(doctorData.AveragePatientHealthIndex);

            patientHealthIndex.append($('<div>').addClass(healthBoxClass).css('background-color', computeColor(doctorData.AveragePatientHealthIndex)).html(healthBoxValue));
            patientHealthIndex.append($('<span/>').text('Patient Average Stress'));


            api.GetCountyHealth(doctorData.CountyInfo.CountyId, function (value) {
                var countyHealthIndex = $('#doctorInfo-countyHealthIndex');
                countyHealthIndex.empty();

                var healthBoxClass = getHealthBoxClass(doctorData.AveragePatientHealthIndex);
                var healthBoxValue = getHealthBoxTextValue(doctorData.AveragePatientHealthIndex);

                countyHealthIndex.append($('<div/>').addClass(healthBoxClass).css('background-color', computeColor(value)).html(healthBoxValue));
                countyHealthIndex.append($('<span/>').text(doctorData.CountyInfo.CountyName + ' average'));
            });

            var patientTable = $('#doctorInfo #patientInfoTable');
            patientTable.empty();

            $('<thead/>')
                .append(
                    $('<tr/>')
                        .append(
                            $('<th/>').text('Patient'))
                        .append(
                            $('<th />').text('Stress'))
                        .append(
                            $('<th />').text('HR'))
                        )
                .appendTo(patientTable);

            $.each(doctorData.Patients, function (id, jObject) {

                var healthIndexClass = getHealthBoxClass(jObject.HealthIndex);
                var healthIndexValue = getHealthBoxTextValue(jObject.HealthIndex);

                var bloodPressureClass = getHealthBoxClass(jObject.HeartRateIndex);
                var bloodPressureValue = getHealthBoxTextValue(jObject.HeartRateIndex);

                $('<tr/>')
                    .append(
                        $('<td/>').text(jObject.PatientName))
                    .append(
                        $('<td/>')
                            .append(
                                $('<div/>').addClass(healthIndexClass).css('background-color', computeColor(jObject.HealthIndex)).html(healthIndexValue)))
                    .append(
                        $('<td/>')
                            .append(
                                $('<div/>').addClass(bloodPressureClass).css('background-color', computeColor(jObject.HeartRateIndex)).html(bloodPressureValue)))
                    .appendTo(patientTable)
            });

            $('#doctorInfo .loadingMessage').hide();

            if (self.currentContext == 'doctor') {
                self.SetUserName(self.currentDoctorName);
            }
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

            api.GetCountyHealth(patientData.CountyInfo.CountyId, function (value) {

                var healthBoxClass = getHealthBoxClass(value);
                var healthBoxText = getHealthBoxTextValue(value);

                var countyHealthIndex = $('#userInfo-countyHealthIndex');
                countyHealthIndex.empty();
                countyHealthIndex.append($('<div/>').addClass(healthBoxClass).css('background-color', computeColor(value)).html(healthBoxText));
                countyHealthIndex.append($('<span/>').text(patientData.CountyInfo.CountyName + ' average'));
            });

            var healthBoxClass = getHealthBoxClass(patientData.HealthIndex);
            var healthBoxText = getHealthBoxTextValue(patientData.HealthIndex);

            var userHealthIndex = $('#userInfo-userHealthIndex');
            userHealthIndex.empty();
            userHealthIndex.append($('<div/>').addClass(healthBoxClass).css('background-color', computeColor(patientData.HealthIndex)).html(healthBoxText));
            userHealthIndex.append($('<span/>').text('Your Stress Index'));

            var heartRateTable = $('#userStats #heartRateTable');
            heartRateTable.empty();

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

    api.GetSetting("GenerateKnownPeople", function (resultValue) {
        if (resultValue.toLowerCase() === "true") {
            metricsApp.InitializeKnownUsers();
        }
        else {
            metricsApp.InitializeRandomUsers();
        }
    });

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

        var projection = d3.geo.albersUsa()
            .scale(width)
            .translate([width / 2, height / 2]);

        var path = d3.geo.path()
            .projection(projection);

        
        d3.json("/Content/us-10m.json", function (error, topology) {
            svg.selectAll("path")
            .data(topojson.feature(topology, topology.objects.counties).features)
            .enter().append("path")
            .attr("d", path)
            .attr('fill', function (d) { return '#313131'; })
            .attr("id", function (d) { return "p" + d.id; });
        });
    },
    refreshMap: function (newData) {
        var self = this;
        $.map(newData, function (data) {
            d3.select("path#p" + data.countyId)
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
        setTimeout(resizeListener, 100); //rebinds itself after 100ms
    });
}

resizeListener();

function computeColor(healthIndex) {
    if (healthIndex >= 0) {
        return 'hsl(' + healthIndex + ', 100%, 50%)';
    }
    else {
        return 'hsl(0, 0%, 0%)';
    }
}

function countyHealthViewModel() {
    this.countyId = 0;
    this.healthIndex = 0;
}

function getHealthBoxClass(value) {
    if (value == -1 || value == 0) {
        return 'healthBox-black';
    }
    else {
        return 'healthBox';
    }
}

function getHealthBoxTextValue(value) {
    if (value == -1) {
        return '0'
    }
    else {
        return value;
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

    ret.countyId = countyHealthData.CountyId;
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
                .success(function (data, status, headers, config) {
                    $scope.countyData = $.map(data, function (countyHealthData) {
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
