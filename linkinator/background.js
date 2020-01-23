/// <reference path="../chrome.d.ts" />
function copy(text) {
  var copyFrom = document.createElement('textarea');
  copyFrom.textContent = text;
  var body = document.getElementsByTagName('body')[0];
  body.appendChild(copyFrom);
  copyFrom.select();
  document.execCommand('copy');
  body.removeChild(copyFrom);
}

function onClicked(tab) {
  var parser = document.createElement('a');
  parser.href = tab.url.toString();
  var relativeUrl = parser.pathname.substring(1) + parser.search;

  console.info('Processing ' + relativeUrl);
  var newUrl = shortenUrl(parser.hostname, relativeUrl);
  // If no replacement was made, copy again the original url.
  if (newUrl.skipped)
    newUrl = tab.url;

  copy(newUrl);

  // Additional probing for currently selected item in specific views that have lists 
  // of work items. We OR them so we stop when the first one is found
  if (
    findSelectedUrl("document.querySelector('.work-items-tab-content .is-selected [data-automation-key=\"System.Title\"] .work-item-title-link').href") ||
    findSelectedUrl("document.querySelector('.grid-row-selected.grid-row-current .work-item-title-link').href") ||
    findSelectedUrl("document.querySelector('.work-item-form-container .workitem-info-bar a').href") ||
    findSelectedUrl("document.querySelector('.work-item-form .workitem-info-bar a').href")) {
    return;
  }
}

function findSelectedUrl(querySelector) {
  chrome.tabs.executeScript({
    "code": querySelector
  }, function (result) {
    href = result[0];
    if (href) {
      var parser = document.createElement('a');
      parser.href = href;
      if (parser.pathname) {
        var shortUrl = parser.pathname.substring(1) + parser.search;
        console.info('Processing ' + shortUrl);
        shortUrl = shortenUrl(parser.hostname, shortUrl);
        copy(shortUrl);
        return true;
      }
    }
  });
}

function shortenUrl(hostname, relativeUrl) {
  var segments = relativeUrl.split('/');
  var org = segments[0];
  var project = segments[1];
  if (hostname == 'devdiv.visualstudio.com') {
    // When using subdomains in visualstudio.com, the subdomain is the 
    // actual organization in dev.azure.com/{org}, and the first path 
    // segment is actually the project.
    org = "DevDiv";
    project = org;
  }

  // ================== Wiki ======================
  if (relativeUrl.includes('/_wiki/wikis/')) {
    // Special case DevDiv: we make it even shorter, and switch domains to 
    // place /DevDiv/DevDiv back server-side
    var domain = relativeUrl.includes('DevDiv/_wiki/wikis/') ? 'http://wiki.devdiv.io/' : 'http://wiki.azdo.io/';
    var match = /_wiki\/wikis\/.*\.wiki\/(\d+)\/(.*)/.exec(relativeUrl);
    if (match) {
      // New short format does not encode the page path but rather uses the page id + its name
      if (org.toLowerCase() == "devdiv" && project.toLowerCase() == "devdiv")
        return domain + match[1] + '/' + match[2];
      else
        return domain + org + '/' + project + '/' + match[1] + '/' + match[2];
    }
  }

  // ================== WorkItems ======================
  if (relativeUrl.includes('/_workitems/edit/'))
    return 'http://work.azdo.io/' + relativeUrl.substring(relativeUrl.indexOf('/_workitems/edit/') + 17);

  if (relativeUrl.includes('workitem=')) {
    var id = /workitem=(\d+)/.exec(relativeUrl);
    return 'http://work.azdo.io/' + id[1];
  }

  // ================== Build ======================
  if (relativeUrl.includes('/_build')) {
    var buildId = /buildId=(\d+)/.exec(relativeUrl);
    var definitionId = /definitionId=(\d+)/.exec(relativeUrl);

    var id = buildId ? parseInt(buildId[1]) : parseInt(definitionId[1]);
    var suffix = '';
    if (org.toLowerCase() == "devdiv" && project.toLowerCase() == "devdiv")
      return 'https://build.azdo.io/' + id;

    if (org.toLowerCase() != "devdiv") {
      // Match build-azdo function ranges to consider IDs BD or builds.
      if (definitionId && id >= 200)
        suffix = '?d';
      else if (buildId && id < 200)
        suffix = '?b';
    }

    return 'https://build.azdo.io/' + org + '/' + project + '/' + id + suffix;
  }

  if (relativeUrl.includes('edit-build-definition&id=')) {
    var buildId = /id=(\d+)/.exec(relativeUrl);
    var id = parseInt(buildId[1]);

    if (org.toLowerCase() == "devdiv" && project.toLowerCase() == "devdiv") {
      return 'https://build.azdo.io/' + id;
    } else {
      // Match build-azdo function ranges to consider IDs BD or builds.
      // A >=200 ID will not be considered a BD by default, so force it in that case.
      return 'https://build.azdo.io/' + org + '/' + project + '/' + id + (id >= 200 ? '?d' : '');
    }
  }

  // ================== Release ======================
  if (relativeUrl.includes('/_releaseDefinition?definitionId=') ||
    (relativeUrl.includes('/_release') && relativeUrl.includes('definitionId='))) {
    // New release pipeline
    var definitionId = /definitionId=(\d+)/.exec(relativeUrl);
    var id = parseInt(definitionId[1]);

    if (org.toLowerCase() == "devdiv" && project.toLowerCase() == "devdiv") {
      return 'http://release.azdo.io/' + id;
    } else {
      // Match build-azdo function ranges to consider IDs RD or releases.
      // A >=50 ID will not be considered an RD by default, so force it in that case.
      return 'http://release.azdo.io/' + org + '/' + project + '/' + id + (id >= 50 ? '?d' : '');
    }
  }

  if ((relativeUrl.includes('/_releaseProgress?') || relativeUrl.includes('release-pipeline-progress')) && relativeUrl.includes('releaseId=')) {
    // New release pipeline
    var releaseId = /releaseId=(\d+)/.exec(relativeUrl);
    var id = parseInt(releaseId[1]);

    if (org.toLowerCase() == "devdiv" && project.toLowerCase() == "devdiv") {
      return 'http://release.azdo.io/' + id;
    } else {
      // Match build-azdo function ranges to consider IDs RD or releases.
      // A <50 ID will not be considered a release by default (an RD instead), so force it in that case.
      return 'http://release.azdo.io/' + org + '/' + project + '/' + id + (id < 50 ? '?r' : '');
    }
  }

  // ================== PullRequest ======================
  if (org.toLowerCase() == "devdiv" && project.toLowerCase() == "devdiv" && relativeUrl.includes('/pullrequest/')) {
    var match = /_git\/(.+)\/pullrequest\/(\d+)/.exec(relativeUrl);
    if (match[1] == 'VS')
      // Make the default project VS, to make it even shorter
      return 'http://pr.devdiv.io/' + match[2];
    else
      return 'http://pr.devdiv.io/' + match[1] + '/' + match[2];
  }

  if (relativeUrl.includes('content/problem/')) {
    var problemId = /problem\/(\d+)\//.exec(relativeUrl);
    return 'http://feedback.devdiv.io/' + problemId[1];
  }

  return { skipped: true };
}

chrome.webNavigation.onCommitted.addListener(function (e) {
  chrome.tabs.query({ active: true, currentWindow: true }, function (tabs) {
    var tab = tabs[0];
    var url = tab.url.replace("devdiv.visualstudio.com/", "dev.azure.com/DevDiv/");
    chrome.tabs.update(tabs[0].id, { url: url });
  });
}, { url: [{ hostSuffix: 'devdiv.visualstudio.com' }] });

chrome.pageAction.onClicked.addListener(onClicked);

chrome.commands.onCommand.addListener(function (command) {
  if (command == "azdo-shorten-url") {
    // Get the currently selected tab
    chrome.tabs.query({ active: true, currentWindow: true }, function (tabs) {
      // Toggle the pinned status
      var current = tabs[0]
      onClicked(current);
    });
  }
});

// When the extension is installed or upgraded ...
chrome.runtime.onInstalled.addListener(function () {
  // Replace all rules ...
  chrome.declarativeContent.onPageChanged.removeRules(undefined, function () {
    // With a new rule ...
    chrome.declarativeContent.onPageChanged.addRules([
      {
        // That fires when a page's URL contains 'devdiv.visualstudio.com' ...
        conditions: [
          new chrome.declarativeContent.PageStateMatcher({
            pageUrl: { urlContains: 'devdiv.visualstudio.com' },
          }),
          new chrome.declarativeContent.PageStateMatcher({
            pageUrl: { urlContains: 'dev.azure.com' },
          }),
          new chrome.declarativeContent.PageStateMatcher({
            pageUrl: { urlContains: 'developercommunity.visualstudio.com/content/problem/' },
          })
        ],
        // And shows the extension's page action.
        actions: [new chrome.declarativeContent.ShowPageAction()]
      }
    ]);
  });
});