﻿/// <reference path="../../_all.ts" />

module dockyard.directives.designerHeader {
    'use strict';

    export interface IDesignerHeaderScope extends ng.IScope {
        onStateChange(): void;
        route: model.RouteDTO
    }

    //More detail on creating directives in TypeScript: 
    //http://blog.aaronholmes.net/writing-angularjs-directives-as-typescript-classes/
    class DesignerHeader implements ng.IDirective {
        public link: (scope: IDesignerHeaderScope, element: ng.IAugmentedJQuery, attrs: ng.IAttributes) => void;
        public controller: ($scope: IDesignerHeaderScope, element: ng.IAugmentedJQuery, attrs: ng.IAttributes) => void;

        public templateUrl = '/AngularTemplate/DesignerHeader';
        public scope = {
            route: '='
        };
        public restrict = 'E';

        constructor(private RouteService: services.IRouteService) {
            DesignerHeader.prototype.link = (
                scope: IDesignerHeaderScope,
                element: ng.IAugmentedJQuery,
                attrs: ng.IAttributes) => {

                //Link function goes here
            };

            DesignerHeader.prototype.controller = (
                $scope: IDesignerHeaderScope,
                $element: ng.IAugmentedJQuery,
                $attrs: ng.IAttributes) => {

                $scope.onStateChange = () => {
                    debugger;
                    if ($scope.route.routeState === model.RouteState.Inactive) {
                        RouteService.deactivate($scope.route);
                    } else {
                        RouteService.activate($scope.route);
                    }
                };
            };
        }

        //The factory function returns Directive object as per Angular requirements
        public static Factory() {
            var directive = (ProcessTemplateService: services.IRouteService) => {
                return new DesignerHeader(ProcessTemplateService);
            };

            directive['$inject'] = ['RouteService'];
            return directive;
        }
    }

    app.directive('designerHeader', DesignerHeader.Factory());
}