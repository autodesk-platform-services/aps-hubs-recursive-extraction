﻿/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by APS Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

$(document).ready(function () {
    // first, check if current visitor is signed in
    jQuery.ajax({
        url: '/api/aps/oauth/token',
        success: function (res) {
            // yes, it is signed in...
            $('#signOut').show();
            $('#refreshHubs').show();

            // prepare sign out
            $('#signOut').click(function () {
                $('#hiddenFrame').on('load', function (event) {
                    location.href = '/api/aps/oauth/signout';
                });
                $('#hiddenFrame').attr('src', 'https://accounts.autodesk.com/Authentication/LogOut');
                // learn more about this signout iframe at
                // https://aps.autodesk.com/blog/log-out-forge
            })

            // and refresh button
            $('#refreshHubs').click(function () {
                $('#userHubs').jstree(true).refresh();
            });

            // finally:
            prepareUserHubsTree();
            showUser();
        }
    });

    $('#autodeskSigninButton').click(function () {
        window.location.replace('/api/aps/oauth/signin');
    });

    $('input[type=radio][name=filter_by]').change(function () {
        itemsTable.refreshTable();
    });

    $('#btnRefresh').click(function () {
        itemsTable.reset();
        itemsTable.getReport();
    });

    $('#executeCSV').click(function () {
        (!!itemsTable ? itemsTable.exportData() : alert("Please, click on a project and wait for the conclusion of the steps before extracting the data!"));
    });

    $.getJSON("/api/aps/oauth/clientid", function (res) {
        $("#ClientID").val(res.id);
        $("#provisionAccountSave").click(function () {
            $('#provisionAccountModal').modal('toggle');
            $('#userHubs').jstree(true).refresh();
        });
    });

});

function prepareUserHubsTree() {
    $('#userHubs').jstree({
        'core': {
            'themes': { "icons": true },
            'multiple': false,
            'data': {
                "url": '/api/aps/datamanagement',
                "dataType": "json",
                'cache': false,
                'data': function (node) {
                    $('#userHubs').jstree(true).toggle_node(node);
                    return { "id": node.id };
                }
            }
        },
        'types': {
            'default': { 'icon': 'glyphicon glyphicon-question-sign' },
            '#': { 'icon': 'glyphicon glyphicon-user' },
            'hubs': { 'icon': 'https://cdn.autodesk.io/dm/a360hub.png' },
            'personalHub': { 'icon': 'https://cdn.autodesk.io/dm/a360hub.png' },
            'bim360Hubs': { 'icon': 'https://cdn.autodesk.io/dm/xs/bim360hub.png' },
            'bim360projects': { 'icon': 'https://cdn.autodesk.io/dm/bim360project.png' },
            'a360projects': { 'icon': 'https://cdn.autodesk.io/dm/a360project.png' },
            'accprojects': {
                'icon': 'https://cdn.autodesk.io/dm/accproject.png'
            },

            'unsupported': { 'icon': 'glyphicon glyphicon-ban-circle' }
        },
        "sort": function (a, b) {
            var a1 = this.get_node(a);
            var b1 = this.get_node(b);
            var parent = this.get_node(a1.parent);
            if (parent.type === 'items') { // sort by version number
                var id1 = Number.parseInt(a1.text.substring(a1.text.indexOf('v') + 1, a1.text.indexOf(':')))
                var id2 = Number.parseInt(b1.text.substring(b1.text.indexOf('v') + 1, b1.text.indexOf(':')));
                return id1 > id2 ? 1 : -1;
            }
            else if (a1.type !== b1.type) return a1.icon < b1.icon ? 1 : -1; // types are different inside folder, so sort by icon (files/folders)
            else return a1.text > b1.text ? 1 : -1; // basic name/text sort
        },
        "plugins": ["types", "state", "sort"],
        "state": { "key": "autodeskHubs" }// key restore tree state
    }).bind("activate_node.jstree", function (evt, data) {
        if (data != null && data.node != null && (data.node.type == 'accprojects' || data.node.type == 'bim360projects')) {
            $('#statusLabel').empty();
            $('#statusLabel').append('<label>reading project ' + data.node.text + '...</label>');
            itemsTable = new ItemsTable("itemsTable", data.node.id.split('/')[6], data.node.id.split('/')[8], data.node.text);
            itemsTable.getReport();
        }
    });
}

function showUser() {
    jQuery.ajax({
        url: '/api/aps/user/profile',
        success: function (profile) {
            var img = '<img src="' + profile.picture + '" height="30px">';
            $('#userInfo').html(img + profile.name);
        }
    });
}
