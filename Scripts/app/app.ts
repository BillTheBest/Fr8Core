/// <reference path="../typings/tsd.d.ts" />
/// <reference path="../typings/metronic.d.ts" />

var app = angular.module("app", [
    "templates",
    "ui.router",
    "ui.bootstrap",
    "oc.lazyLoad",
    "ngSanitize",
    'ngResource',
    'ui.bootstrap',
    "datatables",
    "ngFileUpload",
    "textAngular",
    "ui.select",
    "pusher-angular",
    "ngToast",
    "frapontillo.bootstrap-switch",
    "ApplicationInsightsModule",
    "dndLists",
    "ngTable",
    "mb-scrollbar",
    "ngMessages",
    "ivh.treeview"
]);

/* For compatibility with older versions of script files. Can be safely deleted later. */
app.constant('urlPrefix', '/api');

/* Configure ocLazyLoader(refer: https://github.com/ocombe/ocLazyLoad) */
app.config(['$ocLazyLoadProvider', function ($ocLazyLoadProvider) {
    $ocLazyLoadProvider.config({
        cssFilesInsertBefore: 'ng_load_plugins_before' // load the above css files before a LINK element with this ID. Dynamic CSS files must be loaded between core and theme css files
    });
}]);

/* Setup global settings */
app.factory('settings', ['$rootScope', function ($rootScope) {
    // supported languages
    var settings = {
        layout: {
            pageAutoScrollOnLoad: 1000 // auto scroll to top on page load
        },
        layoutImgPath: Metronic.getAssetsPath() + 'admin/layout/img/',
        layoutCssPath: Metronic.getAssetsPath() + 'admin/layout/css/'
    };

    $rootScope.settings = settings;

    return settings;
}]);

/* Setup App Main Controller */
app.controller('AppController', ['$scope', '$rootScope', function ($scope, $rootScope) {
    $scope.$on('$viewContentLoaded', function () {
        Metronic.initComponents(); // init core components
        //Layout.init(); //  Init entire layout(header, footer, sidebar, etc) on page load if the partials included in server side instead of loading with ng-include directive 
    });
}]);

/***
Layout Partials.
By default the partials are loaded through AngularJS ng-include directive. In case they loaded in server side(e.g: PHP include function) then below partial 
initialization can be disabled and Layout.init() should be called on page load complete as explained above.
***/

/* Setup Layout Part - Header */
app.controller('HeaderController', ['$scope', function ($scope) {
    $scope.$on('$includeContentLoaded', function () {
        Layout.initHeader(); // init header
    });
}]);

/* Setup Layout Part - Footer */
app.controller('FooterController', ['$scope', function ($scope) {
    $scope.$on('$includeContentLoaded', function () {
        Layout.initFooter(); // init footer
    });
}]);

/* Set Application Insights */
app.config(['applicationInsightsServiceProvider', function (applicationInsightsServiceProvider) {
    var options;
    //Temporary instr key (for local instances) until the real one is loaded
    applicationInsightsServiceProvider.configure('e08e940f-1491-440c-8d39-f38e9ff053db', options, true);

    $.get('/api/v1/configuration/appinsights').then((appInsightsInstrKey: string) => {
        console.log(appInsightsInstrKey);
        if (appInsightsInstrKey.indexOf('0000') == -1) { // if not local instance ('Debug' configuration)
            options = { applicationName: 'HubWeb' };
            applicationInsightsServiceProvider.configure(appInsightsInstrKey, options, true);
        } else {
            // don't send telemetry 
            options = {
                applicationName: '',
                autoPageViewTracking: false,
                autoLogTracking: false,
                autoExceptionTracking: false,
                sessionInactivityTimeout: 1
            };
            applicationInsightsServiceProvider.configure(appInsightsInstrKey, options, false);
        }
    });
}]);

/* Setup Rounting For All Pages */
app.config(['$stateProvider', '$urlRouterProvider', '$httpProvider', function ($stateProvider: ng.ui.IStateProvider, $urlRouterProvider, $httpProvider: ng.IHttpProvider) {

    $httpProvider.interceptors.push('fr8VersionInterceptor');

    // Install a HTTP request interceptor that causes 'Processing...' message to display
    $httpProvider.interceptors.push(['$q','$window',($q: ng.IQService, $window: ng.IWindowService) => {
        return {
            request: function (config: ng.IRequestConfig) {
                // Show page spinner If there is no request parameter suppressSpinner.
                if (config && config.params && config.params['suppressSpinner']) {
                    // We don't want this parameter to be sent to backend so remove it if found.
                    delete (config.params.suppressSpinner);
                }
                else {
                    //   Metronic.startPageLoading(<Metronic.PageLoadingOptions>{ animate: true });
                }
                return config;
            },
            response: function (config: ng.IRequestConfig) {
                Metronic.stopPageLoading();
                return config;
            },
            responseError: function (config) {
                if (config.status === 403) {
                    $window.location.href = $window.location.origin + '/DockyardAccount'
                    + '?returnUrl=/Dashboard' + encodeURIComponent($window.location.hash);
                }
                Metronic.stopPageLoading();
                return $q.reject(config);
            }
        }
    }]);

    // Redirect any unmatched url
    $urlRouterProvider.otherwise("/myaccount");

    $stateProvider
        .state('myaccount', {
            url: "/myaccount",
            templateUrl: "/AngularTemplate/MyAccountPage",
            data: { pageTitle: 'My Account', pageSubTitle: '' }
        })
    // Plan list
        .state('planList', {
            url: "/plans",
            templateUrl: "/AngularTemplate/PlanList",
            data: { pageTitle: 'Plans', pageSubTitle: 'This page displays all Plans' }
        })

    // Plan form
        .state('planForm', {
            url: "/plans/{id}",
            templateUrl: "/AngularTemplate/PlanForm",
            data: { pageTitle: 'Plan', pageSubTitle: 'Add a new Plan' },
        })

    // Plan Builder framework
        .state('planBuilder', {
            url: "/plans/{id}/builder?kioskMode",
            //templateUrl: "/AngularTemplate/PlanBuilder",
            views: {
                '@': {
                    templateUrl: ($stateParams: ng.ui.IStateParamsService) => {
                        if ($stateParams['kioskMode']) {
                            return "/AngularTemplate/PlanBuilder_KioskMode";
                        }
                        return "/AngularTemplate/PlanBuilder";
                    }
                },
                'header@': {
                    templateUrl: ($stateParams: ng.ui.IStateParamsService) => {
                        if ($stateParams['kioskMode']) {
                            return "/AngularTemplate/Empty";
                        }
                        return "/AngularTemplate/Header";
                    }
                }
            },
            
            data: { pageTitle: '' }
        })

        .state('showIncidents', {
            url: "/showIncidents",
            templateUrl: "/AngularTemplate/ShowIncidents",
            data: { pageTitle: 'Incidents', pageSubTitle: 'This page displays all incidents' }
        })

        .state('showFacts', {
            url: "/showFacts",
            templateUrl: "/AngularTemplate/ShowFacts",
            data: { pageTitle: 'Facts', pageSubTitle: 'This page displays all facts' },
        })

        .state('planDetails', {
            url: "/plans/{id}/details",
            templateUrl: "/AngularTemplate/PlanDetails",
            data: { pageTitle: 'Plan Details', pageSubTitle: '' }
        })

    // Manage files
        .state('managefiles', {
            url: "/managefiles",
            templateUrl: "/AngularTemplate/ManageFileList",
            data: { pageTitle: 'Manage Files', pageSubTitle: '' }
        })

        .state('fileDetail', {
            url: "/managefiles/{id}",
            templateUrl: "/AngularTemplate/FileDetails",
            data: { pageTitle: 'File details', pageSubTitle: '' }
        })

        .state('accounts', {
            url: '/accounts',
            templateUrl: '/AngularTemplate/AccountList',
            data: { pageTitle: 'Manage Accounts', pageSubTitle: '' }
        })

        .state('accountDetails', {
            url: '/accounts/{id}',
            templateUrl: '/AngularTemplate/AccountDetails',
            data: { pageTitle: 'Account Details', pageSubTitle: '' }
        })

        .state('containerDetails', {
            url: "/container/{id}/details",
            templateUrl: "/AngularTemplate/containerDetails",
            data: { pageTitle: 'Container  Details', pageSubTitle: '' }
        })

        .state('configureSolution', {
            url: "/solution/{solutionName}",
            templateUrl: "/AngularTemplate/PlanBuilder",
            data: { pageTitle: 'Create a Solution', pageSubTitle: '' }
        })

        .state('containers', {
            url: "/containers",
            templateUrl: "/AngularTemplate/ContainerList",
            data: { pageTitle: 'Containers', pageSubTitle: 'This page displays all Containers ' },
        })

        .state('webservices', {
            url: "/webservices",
            templateUrl: "/AngularTemplate/WebServiceList",
            data: { pageTitle: 'Web Services', pageSubTitle: '' }
        })

        .state('findObjects', {
            url: '/findObjects/create',
            templateUrl: '/AngularTemplate/FindObjects',
            data: { pageTitle: 'Constructing Find Objects plan', pageSubTitle: '' }
        })

        .state('findObjectsResult', {
            url: '/findObjects/{id}/results',
            templateUrl: '/AngularTemplate/FindObjectsResults',
            data: { pageTitle: 'Find Objects results', pageSubTitle: '' }
        })

        .state('terminals', {
            url: "/terminals",
            templateUrl: "/AngularTemplate/TerminalList",
            data: { pageTitle: 'Terminals', pageSubTitle: '' }
        })

        .state('manifestregistry', {
            url: "/manifestregistry",
            templateUrl: "/AngularTemplate/ManifestRegistryList",
            data: { pageTitle: 'Manifest Registry', pageSubTitle: '' }
        })

        .state('manageAuthTokens', {
            url: '/manageAuthTokens',
            templateUrl: '/AngularTemplate/ManageAuthTokens',
            data: { pageTitle: 'Manage Auth Tokens', pageSubTitle: '' }
        })

        .state('changePassword', {
            url: '/changePassword',
            templateUrl: '/AngularTemplate/ChangePassword',
            data: { pageTitle: 'Change Password', pageSubTitle: '' }
        })

        .state('reports', {
            url: "/reports",
            templateUrl: "/AngularTemplate/PlanReportList",
            data: { pageTitle: 'Reports', pageSubTitle: 'This page displays all Reports' }
        });
}]);

/* Init global settings and run the app */
app.run(["$rootScope", "settings", "$state", function ($rootScope, settings, $state) {
    $rootScope.$state = $state; // state to be accessed from view
}]);

app.constant('fr8ApiVersion', 'v1');

app.factory('fr8VersionInterceptor', ['fr8ApiVersion', (fr8ApiVersion: string) => {
    var apiPrefix: string = '/api/';
    return {
        'request': (config: ng.IRequestConfig) => {
            //this is an api call, we should append a version to this
            if (config.url.indexOf(apiPrefix) > -1) {
                config.url = config.url.slice(0, 5) + fr8ApiVersion + "/" + config.url.slice(5);
            }
            return config;
        }
    };
}]);


app.config(['ivhTreeviewOptionsProvider', ivhTreeviewOptionsProvider => {
    ivhTreeviewOptionsProvider.set({
        twistieCollapsedTpl: '<span class="glyphicon glyphicon-chevron-right"></span>',
        twistieExpandedTpl: '<span class="glyphicon glyphicon-chevron-down"></span>',
        twistieLeafTpl: '',
        defaultSelectedState: false
    });
}]);