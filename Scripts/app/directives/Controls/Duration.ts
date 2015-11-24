﻿/// <reference path="../../_all.ts" />
module dockyard.directives.duration {
    'use strict';

    export interface IDurationScope extends ng.IScope {
        field: model.DurationControlDefinitionDTO;
    }

    //More detail on creating directives in TypeScript: 
    //http://blog.aaronholmes.net/writing-angularjs-directives-as-typescript-classes/
    class Duration implements ng.IDirective {
        public link: (scope: IDurationScope, element: ng.IAugmentedJQuery, attrs: ng.IAttributes) => void;
        public controller: ($scope: IDurationScope, element: ng.IAugmentedJQuery, attrs: ng.IAttributes) => void;

        public templateUrl = '/AngularTemplate/Duration';
        public restrict = 'E';
        public scope = {
            field: '='
        }

        constructor() {
            Duration.prototype.link = (
                scope: IDurationScope,
                element: ng.IAugmentedJQuery,
                attrs: ng.IAttributes) => {

            }

            Duration.prototype.controller = (
                $scope: IDurationScope,
                $element: ng.IAugmentedJQuery,
                $attrs: ng.IAttributes) => {

                $scope.$watch(() => {
                    if ($scope.field.minutes < 0) {
                        if ($scope.field.hours > 0 || $scope.field.days > 0) {
                            $scope.field.hours--;
                            $scope.field.minutes += 60;
                        } else {
                            $scope.field.minutes = 0;
                        }
                    } else if ($scope.field.minutes >= 60) {
                        $scope.field.hours += Math.floor($scope.field.minutes / 60);
                        $scope.field.minutes %= 60;
                    }

                    if ($scope.field.hours < 0) {
                        if ($scope.field.days > 0) {
                            $scope.field.days--;
                            $scope.field.hours += 24;
                        } else {
                            $scope.field.hours = 0;
                        }
                    } else if ($scope.field.hours >= 24) {
                        $scope.field.days += Math.floor($scope.field.hours / 24);
                        $scope.field.hours %= 24;
                    }
                });

            }

        };

        //The factory function returns Directive object as per Angular requirements
        public static Factory() {
            var directive = () => {
                return new Duration();
            };

            directive['$inject'] = [];
            return directive;
        }
    }

    app.directive('duration', Duration.Factory());
}