function Api() {
    this.serviceUrl = location.protocol + '//' + location.hostname + (location.port ? ':' + location.port : '');

    this.GetNationalStats = function (result) {
        this.httpGetJson(this.serviceUrl + '/api/national/stats', result);
    };

    this.GetNationalHealth = function (result) {
        this.httpGetJson(this.serviceUrl + '/api/national/health', result);
    };

    this.GetCountyHealth = function (countyId, result) {
        this.httpGetJson(this.serviceUrl + '/api/county/' + countyId + '/health', result);
    };

    this.GetDoctors = function (countyId, result) {
        this.httpGetJson('/api/county/' + countyId + '/doctors', result);
    };

    this.GetDoctor = function (doctorId, result) {
        this.httpGetJson(this.serviceUrl + '/api/doctors/' + doctorId, result);
    };

    this.GetPatient = function (bandId, result) {
        this.httpGetJson(this.serviceUrl + '/api/patients/' + bandId, result);
    };

    this.GetSetting = function (settingName, result) {
        this.httpGetJson(this.serviceUrl + '/api/settings/' + settingName, result);
    };

    this.httpGetJson = function (url, result) {
        $.ajax({
            url: url,
            type: 'GET',
            contentType: 'application/json',
            datatype: 'json',
            cache: false
        })
	   .done(function (data) {
	       result(data);
	   })
	   .fail(function () {
	       return;
	   });
    }
}