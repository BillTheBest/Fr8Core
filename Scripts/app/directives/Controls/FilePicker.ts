﻿/// <reference path="../../_all.ts" />
module dockyard.directives.filePicker {
    'use strict';

    export interface IFilePickerScope extends ng.IScope {
        OnFileSelect: ($file: any) => void;
        ListExistingFiles: () => void;
        Save: () => void;
        field: model.FileControlDefinitionDTO;
        selectedFile: interfaces.IFileDescriptionDTO;
    }

    import pca = dockyard.directives.paneConfigureAction;

    //More detail on creating directives in TypeScript: 
    //http://blog.aaronholmes.net/writing-angularjs-directives-as-typescript-classes/
    export function FilePicker(): ng.IDirective {
    //class FilePicker implements ng.IDirective {
        
        

        var controller = ['$scope', '$modal', 'FileService', function ($scope: IFilePickerScope, $modal: any, FileService: IFileService) {

            $scope.selectedFile = null;

            var OnFileUploadSuccess = function(fileDTO: interfaces.IFileDescriptionDTO) {
                $scope.selectedFile = fileDTO;
                $scope.$root.$broadcast("fp-success", fileDTO);
                $scope.field.value = (<dockyard.model.FileDTO>fileDTO).cloudStorageUrl;
                $scope.$root.$broadcast("onChange", new pca.ChangeEventArgs("select_file"));
            }

            var OnFileUploadFail = function(status: any) {
                alert('sorry file upload failed with status: ' + status);
            }

            $scope.OnFileSelect = function($file) {
                FileService.uploadFile($file).then(OnFileUploadSuccess, OnFileUploadFail);
            }

            $scope.Save = function() {
                if ($scope.selectedFile === null) {
                    //raise some kind of error to prevent continuing
                    alert('No file was selected!!!!!!');
                    return;
                }
            
                //we should assign id of selected file to model value
                //this._$scope.field.value = this._fileDTO.id.toString();
                alert('Selected FileDO ID -> ' + $scope.selectedFile.id.toString());
                //TODO add this file's id to CrateDO
            }

            var OnExistingFileSelected = function(fileDTO: interfaces.IFileDescriptionDTO) {
                $scope.selectedFile = fileDTO;
            }

            var OnFilesLoaded = function(filesDTO: Array<interfaces.IFileDescriptionDTO>) {

                var modalInstance = $modal.open({
                    animation: true,
                    templateUrl: '/AngularTemplate/FileSelectorModal',
                    controller: 'FilePicker__FileSelectorModalController',
                    size: 'm',
                    resolve: {
                        files: () => filesDTO
                    }
                });

                modalInstance.result.then(OnExistingFileSelected);
            }

            $scope.ListExistingFiles = function() {
                FileService.listFiles().then(OnFilesLoaded);
            }

        }];

        return {
            restrict: 'E',
            templateUrl: '/AngularTemplate/FilePicker',
            controller: controller,
            scope: {
                field: '='
            }
        };
    }

    app.directive('filePicker', FilePicker);

    app.filter('formatInput', function () {
        return input => {
            if (input) {
                return 'Selected File : ' + input.substring(input.lastIndexOf('/') + 1, input.length)
            }
            return input;
        };
    });

    //TODO talk to alex and move this class to services folder? !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

    interface IFileService {
        uploadFile(file: any): any;
        listFiles(): ng.IPromise<Array<interfaces.IFileDescriptionDTO>>
    }


    /*
        General data persistance methods for FileDirective.
    */
    class FileService implements IFileService {
        constructor(
            private $http: ng.IHttpService,
            private $q: ng.IQService,
            private UploadService: any
            ) { }


        public uploadFile(file: any): any {
            var deferred = this.$q.defer();

            this.UploadService.upload({
                url: '/files',
                file: file
            }).progress((event: any) => {
                console.log('Loaded: ' + event.loaded + ' / ' + event.total);
            })
                .success((fileDTO: interfaces.IFileDescriptionDTO) => {
                 deferred.resolve(fileDTO);
            })
            .error((data: any, status: any) => {
                deferred.reject(status);
            });

            return deferred.promise;
        }

        public listFiles(): ng.IPromise<Array<interfaces.IFileDescriptionDTO>> {
            var deferred = this.$q.defer();
            this.$http.get<Array<interfaces.IFileDescriptionDTO>>('/files').then(resp => {
                deferred.resolve(resp.data);
            }, err => {
                deferred.reject(err);
            });
            return deferred.promise;
            
        }
    }

    /*
        Register FileService with AngularJS. Upload dependency comes from ng-file-upload module
    */
    app.factory('FileService', ['$http', '$q', 'Upload',
        ($http, $q, UploadService) => {
            return new FileService($http, $q, UploadService);
    }]);

    /*
    A simple controller for Listing existing files dialog.
    Note: here goes a simple (not really a TypeScript) way to define a controller. 
    Not as a class but as a lambda function.
*/
    app.controller('FilePicker__FileSelectorModalController', ['$scope', '$modalInstance', 'files', ($scope: any, $modalInstance: any, files: Array<interfaces.IFileDescriptionDTO>): void => {

        $scope.files = files;

        $scope.selectFile = (file: interfaces.IFileDescriptionDTO) => {
            $modalInstance.close(file);
        };

        $scope.cancel = () => {
            $modalInstance.dismiss();
        };

    }]);

}