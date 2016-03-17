﻿module dockyard.services {
    export class UpstreamExtractor {
        constructor(
            private $http: ng.IHttpService,
            private $q: ng.IQService
        ) {
        }

        // So far extracts only Fields Descriptions.
        public extractUpstreamData(activityId: string, manifestType: string, availability: string) {
            var defer = this.$q.defer();

            var url = '/api/plannodes/upstream_fields/?id=' + activityId
                + '&manifestType=' + manifestType
                + '&availability=' + availability;

            this.$http.get(url)
                .then((res) => {
                    defer.resolve(res.data);
                });

            return defer.promise;
        }
    }
}

app.service('UpstreamExtractor', [ '$http', '$q', dockyard.services.UpstreamExtractor ]); 
