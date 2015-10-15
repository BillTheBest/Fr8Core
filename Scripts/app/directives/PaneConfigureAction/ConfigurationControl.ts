﻿/// <reference path="../../_all.ts" />
module dockyard.directives.paneConfigureAction {
    'use strict';

    enum FieldType {
        textField,
        checkboxField,
        filePicker,
        radioGroupButton,
        dropdownlistField,
        textBlockField,
        routing,
        button
    }

    export class ChangeEventArgs {
        constructor(fieldName: string) {
            this.fieldName = fieldName;
        }

        public fieldName: string;
    }

    export interface IConfigurationControlScope extends ng.IScope {
        field: model.ControlDefinitionDTO;
        onChange: (radio: model.ControlDefinitionDTO) => void;
        onClick: (event: any) => void;
    }

    //More detail on creating directives in TypeScript: 
    //http://blog.aaronholmes.net/writing-angularjs-directives-as-typescript-classes/
    class ConfigurationControl implements ng.IDirective {
        public link: (scope: IConfigurationControlScope, element: ng.IAugmentedJQuery, attrs: ng.IAttributes) => void;
        public controller: ($scope: IConfigurationControlScope, element: ng.IAugmentedJQuery, attrs: ng.IAttributes) => void;
        public scope = {
            currentAction: '=',
            field: '='
        };
        public templateUrl = '/AngularTemplate/ConfigurationControl';
        public restrict = 'E';

        constructor() {
            ConfigurationControl.prototype.link = (
                $scope: IConfigurationControlScope,
                $element: ng.IAugmentedJQuery,
                $attrs: ng.IAttributes) => {
            };

            ConfigurationControl.prototype.controller = (
                $scope: IConfigurationControlScope,
                $element: ng.IAugmentedJQuery,
                $attrs: ng.IAttributes) => {
                
                $scope.onChange = (event: any) => {
                    var fieldName: string;

                    if (!!event.target === true) {
                        // If called by DOM event (for standard fields), get field name
                        // Get name of field that received the event
                        fieldName = event.target.attributes.getNamedItem('data-field-name').value;
                    }
                    else {
                        // If called by custom field, it is assumed that field name is suppied as the argument
                        fieldName = event;
                    }

                    $scope.$emit("onChange", new ChangeEventArgs(fieldName));
                };


                $scope.onClick = (event: any) => {
                    var fieldName: string;

                    if (!!event.target === true) {
                        // If called by DOM event (for standard fields), get field name
                        // Get name of field that received the event
                        fieldName = event.target.attributes.getNamedItem('data-field-name').value;
                    }
                    else {
                        // If called by custom field, it is assumed that field name is suppied as the argument
                        fieldName = event;
                    }

                    $scope.$emit("onClick", new ChangeEventArgs(fieldName));
                };

            };
        }

        //The factory function returns Directive object as per Angular requirements
        public static Factory() {
            var directive = () => {
                return new ConfigurationControl();
            };

            directive['$inject'] = [];
            return directive;
        }
    }

    app.directive('configurationControl', ConfigurationControl.Factory());

    // A simple filter to format a string as a valid HTML identifier
    // per http://www.w3.org/TR/html4/types.html#type-id 
    app.filter('validId', function () {
        return function (input) {
            if (input) {
                return input.replace(/^[^a-zA-Z]/, 'a').replace(/[^\w\d\-_\.:]/g, '-');
            }
        }
    });

}