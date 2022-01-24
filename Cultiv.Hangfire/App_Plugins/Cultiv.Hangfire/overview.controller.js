(function () {
    "use strict";

    function HangfireOverviewController($scope, $location, $routeParams, localizationService) {

        const vm = this;

        vm.page = {};
        vm.page.labels = {};
        vm.page.name = "";
        vm.page.navigation = [];

        onInit();

        function onInit() {

            loadNavigation();

            setPageName();
        }

        function loadNavigation() {

            var labels = ["sections_hangfire"];

            localizationService.localizeMany(labels).then(data => {
                vm.page.labels.hangfire = data[0];

                vm.page.navigation = [
                    {
                        "name": vm.page.labels.hangfire,
                        "icon": "icon-repeat",
                        "view": Umbraco.Sys.ServerVariables.umbracoSettings.appPluginsPath + '/Cultiv.Hangfire/backoffice/hangfire/views/dashboard.html',
                        "active": true,
                        "alias": "hangfire",
                        "action": function () {
                            $location.path("/hangfire/hangfire/overview");
                        }
                    }
                ];
            });
        }

        function setPageName() {
            localizationService.localize("sections_hangfire").then(data => {
                vm.page.name = data;
            })
        }
    }

    angular.module("umbraco").controller("Cultiv.Hangfire.OverviewController", HangfireOverviewController);

})();