﻿/// <reference path="../../../../app/_all.ts" />
/// <reference path="../../../../typings/angularjs/angular-mocks.d.ts" />


module dockyard.tests.unit.directives.controls {
    import fx = utils.fixtures; // just an alias

    var compileTemplate = (localScope, rawTemplate, $compile) => {
        var template = angular.element(rawTemplate);
        var elem = $compile(template)(localScope);
        localScope.$digest();
        return elem;
    };

    var getInputs = (curElement, selector) => {
        return angular.element(curElement.find(selector));
    };

    var triggerBlurHandler = (scope, curElement, selector) => {
        getInputs(curElement, selector).val('test value');
        getInputs(curElement, selector).blur();
        scope.$apply();
    };

    describe('Testing TextSource control', () => {
        var $rootScope,
            $compile,
            $timeout,
            element,
            scope,
            directive = '<text-source change="onChange" field= "field" />';
        

        beforeEach(module('app', 'templates'));

        app.run(['$httpBackend', (_$httpBackend_) => {
            //we need this because stateProvider loads on test startup and routes us to default state 
            //which is myaccount and has template URL with /AngularTemplate/MyAccountPage
            _$httpBackend_.expectGET('/AngularTemplate/MyAccountPage').respond(200, '<div></div>');
        }]);


        beforeEach(() => {

            inject((_$compile_, _$rootScope_, _$timeout_) => {
                $rootScope = _$rootScope_;
                $compile = _$compile_;
                $timeout = _$timeout_;

                scope = $rootScope.$new();
                //we copy data to prevent changes on ongoing tests
                scope.field = angular.copy(fx.FieldDTO.textSource);
                element = compileTemplate(scope, directive, $compile);
            });
        });

        it('Should have an isolateScope', () => {
            expect(element.isolateScope()).not.toBe(null);
        });

        it('Should have an radio button with label a specific value', () => {
            var curScope = element.isolateScope();
            expect(element.find("[value='specific']").length).toBe(1);
        });

        it('Should contain the a radio button for enabling upstream crate dropdown', () => {
            var curScope = element.isolateScope();
            expect(element.find("[value='upstream']").length).toBe(1);
        });

        it('Should contain the a dropdown list for selecting value from upstream crate', () => {
            expect(element.find("drop-down-list-box").length).toBe(1);
        });

        it('Should call onchange of input field on blur', () => {
            var curScope = element.isolateScope();

            // logic for checking onChange function call
            curScope.onChange = jasmine.createSpy("onChange");
            triggerBlurHandler(curScope, element, ".form-control-focus");
            expect(curScope.onChange).toHaveBeenCalled();
        });
    });


    describe('Testing TextSource multi usage', () => {
        var $rootScope,
            $compile,
            $timeout,
            element1,
            element2,
            scope,
            directive1 = '<text-source change="onChange" field= "field1" />',
            directive2 = '<text-source change="onChange" field= "field2" />';


        beforeEach(module('app', 'templates'));

        app.run(['$httpBackend', (_$httpBackend_) => {
            //we need this because stateProvider loads on test startup and routes us to default state 
            //which is myaccount and has template URL with /AngularTemplate/MyAccountPage
            _$httpBackend_.expectGET('/AngularTemplate/MyAccountPage').respond(200, '<div></div>');
        }]);


        beforeEach(() => {

            inject((_$compile_, _$rootScope_, _$timeout_) => {
                $rootScope = _$rootScope_;
                $compile = _$compile_;
                $timeout = _$timeout_;
                scope = $rootScope.$new();

                scope.field1 = angular.copy(fx.FieldDTO.textSource);
                scope.field2 = angular.copy(fx.FieldDTO.textSource);
                element1 = compileTemplate(scope, directive1, $compile);
                element2 = compileTemplate(scope, directive2, $compile);


            });
        });

        it('Should contain the drop-down for upstream crate in it\'s own scope', () => {
            expect(element1.find("drop-down-list-box").length).toBe(1);
            expect(element2.find("drop-down-list-box").length).toBe(1);
        });

        it('Should call onChange of input field on blur in it\'s own scope', () => {

            var curScope1 = element1.isolateScope();
                
            // logic for checking onChange function call
            curScope1.onChange = jasmine.createSpy("onChange");
            triggerBlurHandler(curScope1, element1, ".form-control-focus");
            expect(curScope1.onChange).toHaveBeenCalled();

            var curScope2 = element2.isolateScope();
            curScope2.onChange = jasmine.createSpy("onChange");
            triggerBlurHandler(curScope2, element2, ".form-control-focus");
            expect(curScope2.onChange).toHaveBeenCalled();
        });
       
    });
}
