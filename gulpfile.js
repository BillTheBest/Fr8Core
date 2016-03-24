    var gulp = require('gulp');
var bower = require('gulp-bower');
var concat = require('gulp-concat');
var sourcemaps = require('gulp-sourcemaps');
var templateCache = require('gulp-angular-templatecache');

gulp.task('bower', function (done) {
    return bower({ layout: "byComponent" });
});

gulp.task('concattemplates', function () {
    return gulp.src(['Views/AngularTemplate/**/*.cshtml',
        /*we are excluding those files - because they contain razor code*/
        '!Views/AngularTemplate/TerminalList.cshtml',
        '!Views/AngularTemplate/PlanList.cshtml',
        '!Views/AngularTemplate/MyAccountPage.cshtml',
        '!Views/AngularTemplate/Header.cshtml',
        '!Views/AngularTemplate/ChangePassword.cshtml',
        '!Views/AngularTemplate/AccountList.cshtml'])
        .pipe(templateCache('templateCache.js', {
            module: 'templates',
            standalone: true,
            transformUrl: function(url) {
                //remove .cshtml extension and /AngularTemplate/ prefix
                return '/AngularTemplate/' + url.slice(0, -7);
            }
        }))
        .pipe(gulp.dest('Scripts/tests/templates'))
        .pipe(gulp.dest('Scripts'));
});

gulp.task('compile_js', function () {
    return gulp.src([
        'Scripts/app/model/ActionDTO.js',
        'Scripts/app/model/ActivityTemplate.js',
        'Scripts/app/model/Condition.js',
        'Scripts/app/model/Criteria.js',
        'Scripts/app/model/Field.js',
        'Scripts/app/model/SubPlan.js',
        'Scripts/app/model/ControlsList.js',
        'Scripts/app/model/CrateStorage.js',
        'Scripts/app/model/FieldMappingSettings.js',
        'Scripts/app/model/Plan.js',
        'Scripts/app/model/PlanBuilderState.js',
        'Scripts/app/model/User.js',
        'Scripts/app/model/ContainerDTO.js',
        'Scripts/app/model/ActionGroup.js',
        'Scripts/app/model/WebServiceDTO.js',
        'Scripts/app/model/WebServiceActionSetDTO.js',
        'Scripts/app/model/TerminalDTO.js',
        'Scripts/app/model/TerminalActionSetDTO.js',
        'Scripts/app/model/ManageAuthToken.js',
        'Scripts/app/model/SolutionDTO.js',
        'Scripts/app/model/AlertDTO.js',
        'Scripts/app/services/CrateHelper.js',
        'Scripts/app/services/AuthService.js',
        'Scripts/app/services/ConfigureTrackerService.js',
        'Scripts/app/services/PlanBuilderService.js',
        'Scripts/app/services/StringService.js',
        'Scripts/app/services/LocalIdentityGenerator.js',
        'Scripts/app/services/ReportService.js',
        'Scripts/app/services/ManageFileService.js',
        'Scripts/app/services/FileDetailsService.js',
        'Scripts/app/services/ContainerService.js',
        'Scripts/app/services/UIHelperService.js',
        'Scripts/app/services/LayoutService.js',
        'Scripts/app/services/PusherNotifierService.js',
        'Scripts/app/services/UserService.js',
        'Scripts/app/services/WebServiceService.js',
        'Scripts/app/services/TerminalService.js',
        'Scripts/app/services/ManageAuthTokenService.js',
        'Scripts/app/services/ManifestRegistryService.js',
        'Scripts/app/services/SolutionDocumentationService.js',
        'Scripts/app/services/UpstreamExtractor.js',
        'Scripts/app/filters/PlanState.js',
        'Scripts/app/filters/ContainerState.js',
        'Scripts/app/filters/FilterByTag.js',
        'Scripts/app/directives/EventArgsBase.js',
        'Scripts/app/directives/directives.js',
        'Scripts/app/directives/indiClick.js',
        'Scripts/app/directives/layout.js',
        'Scripts/app/directives/PaneWorkflowDesigner/Messages.js',
        'Scripts/app/directives/PaneWorkflowDesigner/PaneWorkflowDesigner.js',
        'Scripts/app/directives/PaneConfigureAction/PaneConfigureAction.js',
        'Scripts/app/directives/PaneConfigureAction/ConfigurationControl.js',
        'Scripts/app/directives/PaneSelectAction/PaneSelectAction.js',
        'Scripts/app/directives/DesignerHeader/DesignerHeader.js',
        'Scripts/app/directives/SubplanHeader.js',
        'Scripts/app/directives/Controls/FilePicker.js',
        'Scripts/app/directives/Controls/RadioButtonGroup.js',
        'Scripts/app/directives/Controls/DropDownListBox.js',
        'Scripts/app/directives/Controls/TextBlock.js',
        'Scripts/app/directives/Controls/TextArea.js',
        'Scripts/app/directives/Controls/TextArea.js',
        'Scripts/app/directives/Controls/FilterPane.js',
        'Scripts/app/directives/QueryBuilderWidget.js',
        'Scripts/app/directives/Controls/MappingPane.js',
        'Scripts/app/directives/Controls/ManagePlan.js',
        'Scripts/app/directives/Controls/RunPlanButton.js',
        'Scripts/app/directives/Controls/FieldList.js',
        'Scripts/app/directives/Controls/QueryBuilder.js',
        'Scripts/app/directives/Controls/TextSource.js',
        'Scripts/app/directives/Controls/SourceableCriteria.js',
        'Scripts/app/directives/Controls/InputFocus.js',
        'Scripts/app/directives/Controls/Counter.js',
        'Scripts/app/directives/Controls/TimePicker.js',
        'Scripts/app/directives/Controls/Duration.js',
        'Scripts/app/directives/Controls/DatePicker.js',
        'Scripts/app/directives/Controls/UpstreamDataChooser.js',
        'Scripts/app/directives/Controls/UpstreamFieldChooser.js',
        'Scripts/app/directives/Controls/UpstreamCrateChooser.js',
        'Scripts/app/directives/Controls/CrateChooser.js',
        'Scripts/app/directives/Controls/ContainerTransition.js',
        'Scripts/app/directives/Controls/ControlContainer.js',
        'Scripts/app/directives/Controls/ControlList.js',
        'Scripts/app/directives/LongAjaxCursor.js',
        'Scripts/app/directives/Validators/ManifestDescriptionValidators.js',
        'Scripts/app/directives/ActionPicker.js',
        'Scripts/app/directives/Resizable.js',
        'Scripts/app/filters/ActionNameFormatter.js',
        'Scripts/app/filters/DateTimeFormatter.js',
        'Scripts/app/controllers/PlanBuilderController.js',
        'Scripts/app/controllers/SandboxController.js',
        'Scripts/app/controllers/PlanFormController.js',
        'Scripts/app/controllers/PlanListController.js',
        'Scripts/app/controllers/ReportFactController.js',
        'Scripts/app/controllers/ReportIncidentController.js',
        'Scripts/app/controllers/PlanDetailsController.js',
        'Scripts/app/controllers/ManageFileListController.js',
        'Scripts/app/controllers/FileDetailsController.js',
        'Scripts/app/controllers/AccountListController.js',
        'Scripts/app/controllers/AccountDetailsController.js',
        'Scripts/app/controllers/InternalAuthenticationController.js',
        'Scripts/app/controllers/AuthenticationDialogController.js',
        'Scripts/app/controllers/SelectActionController.js',
        'Scripts/app/controllers/ContainerListController.js',
        'Scripts/app/controllers/ContainerDetailsController.js',
        'Scripts/app/controllers/WebServiceListController.js',
        'Scripts/app/controllers/WebServiceFormController.js',
        'Scripts/app/controllers/TerminalListController.js',
        'Scripts/app/controllers/TerminalFormController.js',
        'Scripts/app/controllers/SolutionListController.js',
        'Scripts/app/controllers/NotifierController.js',
        'Scripts/app/controllers/PlanActionsDialogController.js',
        'Scripts/app/controllers/FindObjectsController.js',
        'Scripts/app/controllers/FindObjectsResultsController.js',
        'Scripts/app/controllers/ManageAuthTokenController.js',
        'Scripts/app/controllers/PayloadFormController.js',
        'Scripts/app/controllers/TerminalListController.js',
        'Scripts/app/controllers/TerminalFormController.js',
        'Scripts/app/controllers/ManageAuthTokenController.js',
        'Scripts/app/controllers/ManifestRegistryListController.js',
        'Scripts/app/controllers/ManifestRegistryFormController.js',
        'Scripts/app/controllers/SolutionDocumentationController.js',
        'Scripts/app/controllers/ManageUserController.js'
    ])
        .pipe(sourcemaps.init())
        .pipe(concat('_compiled.js'))
        .pipe(sourcemaps.write())
        .pipe(gulp.dest('Scripts/app'));
});

gulp.task('watch_js', ['compile_js'], function () {
    gulp.watch('Scripts/app/**/*.js', ['compile_js']);
});

var cdnizer = require("gulp-cdnizer");

gulp.task('cdnizer-css', ['bower'], function () {
    return gulp.src('./Views/Shared/_AngularAppStyles.cshtml')
        .pipe(cdnizer([
            {
                file: '~/bower_components/bootstrap/dist/css/bootstrap.css',
                package: 'bootstrap',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/${ version }/css/bootstrap.min.css'
            },
            {
                file: '~/bower_components/awesome-bootstrap-checkbox/awesome-bootstrap-checkbox.css',
                package: 'awesome-bootstrap-checkbox',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/awesome-bootstrap-checkbox/${ version }/awesome-bootstrap-checkbox.min.css'
            },
            {
                file: '~/bower_components/bootstrap-switch/dist/css/bootstrap3/bootstrap-switch.css',
                package: 'bootstrap-switch',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/bootstrap-switch/${ version }/css/bootstrap3/bootstrap-switch.min.css'
            },
            {
                file: '~/bower_components/datatables/media/css/jquery.dataTables.css',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/datatables/1.10.11/css/jquery.dataTables.min.css'
            },
            {
                file: '~/bower_components/datatables/media/css/dataTables.bootstrap.min.css',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/datatables/1.10.11/css/dataTables.bootstrap.min.css'
            },
            {
                file: '~/bower_components/textAngular/dist/textAngular.css',
                package: 'textAngular',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/textAngular/${ version }/textAngular.css'
            },
            {
                file: '~/bower_components/ng-table/dist/ng-table.min.css',
                package: 'ng-table',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/ng-table/${ version }/ng-table.min.css'
            },
            {
                file: '~/bower_components/angular-ui-select/dist/select.min.css',
                package: 'angular-ui-select',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/angular-ui-select/${ version }/select.min.css'
            },
            {
                file: '~/bower_components/jquery-ui/themes/smoothness/jquery-ui.min.css',
                package: 'jquery-ui',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/jqueryui/${ version }/jquery-ui.min.css'
            }
        ]))
        .pipe(gulp.dest('./Views/Shared/CDN'));
});


gulp.task('cdnizer-js', ['bower'], function () {
    return gulp.src('./Views/Shared/_AngularAppScripts.cshtml')
        .pipe(cdnizer([
            {
                file: '~/bower_components/jquery/dist/jquery.js',
                package: 'jquery',
                cdn: '//ajax.googleapis.com/ajax/libs/jquery/${ version }/jquery.min.js'
            },
            {
                file: '~/bower_components/jquery-ui/jquery-ui.min.js',
                package: 'jquery-ui',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/jqueryui/${ version }/jquery-ui.min.js'
            },
            {
                file: '~/bower_components/jquery-migrate/jquery-migrate.js',
                package: 'jquery-migrate',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/jquery-migrate/${ version }/jquery-migrate.min.js'
            },
            {
                file: '~/bower_components/bootstrap/dist/js/bootstrap.js',
                package: 'bootstrap',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/twitter-bootstrap/${ version }/js/bootstrap.min.js'
            },
            {
                file: '~/bower_components/bootstrap-hover-dropdown/bootstrap-hover-dropdown.js',
                package: 'bootstrap-hover-dropdown',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/bootstrap-hover-dropdown/${ version }/bootstrap-hover-dropdown.min.js'
            },
            {
                file: '~/bower_components/spin.js/spin.js',
                package: 'spin.js',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/spin.js/${ version }/spin.min.js'
            },
            {
                file: '~/bower_components/bootstrap-switch/dist/js/bootstrap-switch.js',
                package: 'bootstrap-switch',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/bootstrap-switch/${ version }/js/bootstrap-switch.min.js'
            },
            {
                file: '~/bower_components/angular/angular.js',
                package: 'angular',
                test: 'window.angular',
                cdn: '//ajax.googleapis.com/ajax/libs/angularjs/${ version }/angular.min.js'
            },
            {
                file: '~/bower_components/angular-resource/angular-resource.js',
                package: 'angular-resource',
                cdn: '//ajax.googleapis.com/ajax/libs/angularjs/${ version }/angular-resource.min.js'
            },
            {
                file: '~/bower_components/angular-animate/angular-animate.js',
                package: 'angular',                
                cdn: '//ajax.googleapis.com/ajax/libs/angularjs/${ version }/angular-animate.min.js'
            },
            {
                file: '~/bower_components/angular-sanitize/angular-sanitize.js',
                package: 'angular-sanitize',
                cdn: '//ajax.googleapis.com/ajax/libs/angularjs/${ version }/angular-sanitize.min.js'
            },
            {
                file: '~/bower_components/angular-ui-router/release/angular-ui-router.js',
                package: 'angular-ui-router',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/angular-ui-router/${ version }/angular-ui-router.min.js'
            },

            {
                file: '~/bower_components/angular-bootstrap/ui-bootstrap-tpls.js',
                package: 'ui-bootstrap',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/angular-ui-bootstrap/${ version }/ui-bootstrap-tpls.min.js'
            },
            {
                file: '~/bower_components/underscore/underscore.js',
                package: 'underscore',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/underscore.js/${ version }/underscore-min.js'
            },
            {
                file: '~/bower_components/datatables/media/js/jquery.dataTables.min.js',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/datatables/1.10.11/js/jquery.dataTables.min.js'
            },
            {
                file: '~/bower_components/ng-table/dist/ng-table.min.js',
                package: 'ng-table',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/ng-table/${ version }/ng-table.min.js'
            },
            {
                file: '~/bower_components/ng-file-upload/ng-file-upload-all.min.js',
                package: 'ng-file-upload',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/danialfarid-angular-file-upload/${ version }/ng-file-upload-all.min.js'
            },
            {
                file: '~/bower_components/pusher/dist/pusher.js',
                package: 'pusher',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/pusher/${ version }/pusher.min.js'
            },
            {
                file: '~/bower_components/pusher-angular/lib/pusher-angular.js',
                package: 'pusher-angular',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/pusher-angular/${ version }/pusher-angular.min.js'
            },
            {
                file: '~/bower_components/rangy/rangy-core.min.js',
                package: 'rangy',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/rangy/${ version }/rangy-core.min.js'
            },
            {
                file: '~/bower_components/rangy/rangy-selectionsaverestore.min.js',
                package: 'rangy',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/rangy/${ version }/rangy-selectionsaverestore.min.js'
            },
            {
                file: '~/bower_components/textAngular/dist/textAngular-sanitize.min.js',
                package: 'textAngular',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/textAngular/${ version }/textAngular-sanitize.min.js'
            },
            {
                file: '~/bower_components/textAngular/dist/textAngular.min.js',
                package: 'textAngular',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/textAngular/${ version }/textAngular.min.js'
            },
            {
                file: '~/bower_components/angular-ui-select/dist/select.min.js',
                package: 'angular-ui-select',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/angular-ui-select/${ version }/select.min.js'
            },
            {
                file: '~/bower_components/dndLists/angular-drag-and-drop-lists.min.js',
                package: 'dndLists',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/angular-drag-and-drop-lists/${ version }/angular-drag-and-drop-lists.min.js'
            },
            {
                file: '~/bower_components/angular-messages/angular-messages.min.js',
                package: 'angular-messages',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/angular-messages/${ version }/angular-messages.min.js'
            },
            {
                file: '~/Scripts/lib/jquery.blockui.min.js',
                cdn: '//cdnjs.cloudflare.com/ajax/libs/jquery.blockUI/2.70/jquery.blockUI.min.js'
            }
        ]))
        .pipe(gulp.dest('./Views/Shared/CDN'));
});

gulp.task('default', ['bower', 'concattemplates', 'cdnizer-js', 'cdnizer-css']);