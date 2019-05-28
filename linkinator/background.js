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
  var shortUrl = parser.pathname.substring(1) + parser.search;

  console.info('Processing ' + shortUrl);
  var newUrl = shortenUrl(parser.hostname, shortUrl);
  // If no replacement was made, copy again the original url.
  if (newUrl.skipped)
    newUrl = tab.url;

  copy(newUrl);

  chrome.tabs.executeScript(tab.id, {
      "code": "if (window.jQuery) { $('.workitem-dialog a.caption').attr('href'); } else { '' }"
    }, function (result) {
      href = result[0];
      if (href) {
        var shortUrl = href.pathname.substring(1) + href.search;
        console.info('Processing ' + shortUrl);
        shortUrl = shortenUrl(href.hostname, shortUrl);
        copy(shortUrl);
      }
    });
}

function shortenUrl(hostname, shortUrl) {
  var segments = shortUrl.split('/');
  var org = segments[0];
  var project = segments[1];
  if (hostname == 'devdiv.visualstudio.com')
  {
    // When using subdomains in visualstudio.com, the subdomain is the 
    // actual organization in dev.azure.com/{org}, and the first path 
    // segment is actually the project.
    org = "DevDiv";
    project = org;
  }

  if (shortUrl.includes('/_wiki/wikis/')) {
    var indexOfPagePath = shortUrl.indexOf('pagePath=') + 9;
    var indexOfEndPath = shortUrl.indexOf('&', indexOfPagePath);
    // Special case DevDiv: we make it even shorter, and switch domains to 
    // place /DevDiv/DevDiv back server-side
    var domain = shortUrl.includes('DevDiv/_wiki/wikis/') ? 'http://wiki.devdiv.io/' : 'http://wiki.azdo.io/';
    var pagePath = indexOfEndPath == -1 ? shortUrl.substring(indexOfPagePath) : shortUrl.substring(indexOfPagePath, indexOfEndPath);
    pagePath = decodeURIComponent(pagePath).replace(/^\//, "");

    if (org == "DevDiv" && project == "DevDiv")
      return domain + pagePath;
    else 
      return domain + org + '/' + project + '/' + pagePath;
  }

  // We could make wiki pages way shorter by just using the id instead... 
  // if (shortUrl.includes('/_wiki/wikis/DevDiv.wiki/') && shortUrl.includes('pageId=')) {
  //   var pageId = /pageId=(\d+)/.exec(shortUrl);
  //   return 'http://wiki.devdiv.io/' + pageId;
  // }

  if (shortUrl.includes('/_workitems/edit/'))
    return 'http://work.azdo.io/' + shortUrl.substring(shortUrl.indexOf('/_workitems/edit/') + 17);
    
  if (shortUrl.includes('/_build')) {
    var buildId = /buildId=(\d+)/.exec(shortUrl);
    var definitionId = /definitionId=(\d+)/.exec(shortUrl);

    var id = buildId ? parseInt(buildId[1]) : parseInt(definitionId[1]);
    var suffix = '';
    if (org == "DevDiv" && project == "DevDiv")
      return 'http://build.azdo.io/' + id;
    
    if (org != "DevDiv") {
      // Match build-azdo function ranges to consider IDs BD or builds.
      if (definitionId && id >= 200)
        suffix = '?d';
      else if (buildId && id < 200)
        suffix = '?b';
    }

    return 'http://build.azdo.io/' + org + '/' + project + '/' + id + suffix;
  }

  if (shortUrl.includes('edit-build-definition&id=')) {
    var buildId = /id=(\d+)/.exec(shortUrl);
    var id = parseInt(buildId[1]);

    if (org == "DevDiv" && project == "DevDiv") {
      return 'http://build.azdo.io/' + id;
    } else {
      // Match build-azdo function ranges to consider IDs BD or builds.
      // A >=200 ID will not be considered a BD by default, so force it in that case.
      return 'http://build.azdo.io/' + org + '/' + project + '/' + id + (id >= 200 ? '?d' : '');
    }
  }

  if (shortUrl.includes('/_releaseDefinition?definitionId=') || shortUrl.includes('/_release?definitionId=')) {
    // New release pipeline
    var definitionId = /definitionId=(\d+)/.exec(shortUrl);
    var id = parseInt(definitionId[1]);

    if (org == "DevDiv" && project == "DevDiv") {
      return 'http://release.azdo.io/' + id;
    } else {
      // Match build-azdo function ranges to consider IDs RD or releases.
      // A >=50 ID will not be considered an RD by default, so force it in that case.
      return 'http://release.azdo.io/' + org + '/' + project + '/' + id + (id >= 50 ? '?d' : '');
    }
  }

  if (shortUrl.includes('/_releaseProgress?') && shortUrl.includes('releaseId=')) {
    // New release pipeline
    var releaseId = /releaseId=(\d+)/.exec(shortUrl);
    var id = parseInt(releaseId[1]);

    if (org == "DevDiv" && project == "DevDiv") {
      return 'http://release.azdo.io/' + id;
    } else {
      // Match build-azdo function ranges to consider IDs RD or releases.
      // A <50 ID will not be considered a release by default (an RD instead), so force it in that case.
      return 'http://release.azdo.io/' + org + '/' + project + '/' + id + (id < 50 ? '?r' : '');
    }
  }

  if (org == "DevDiv" && project == "DevDiv" && shortUrl.includes('/pullrequest/')) {
    var match = /_git\/(.+)\/pullrequest\/(\d+)/.exec(shortUrl);
    if (match[1] == 'VS') 
      // Make the default project VS, to make it even shorter
      return 'http://pr.devdiv.io/' + match[2];
    else
      return 'http://pr.devdiv.io/' + match[1] + '/' + match[2];
  }

  if (shortUrl.includes('content/problem/')) {
    var problemId = /problem\/(\d+)\//.exec(shortUrl);
    return 'http://feedback.devdiv.io/' + problemId[1];
  }
  
  return { skipped: true };
}

chrome.webNavigation.onCommitted.addListener(function(e) {
  chrome.tabs.query( { active: true, currentWindow: true }, function( tabs ) {
    var tab = tabs[0];
    var url = tab.url.replace("devdiv.visualstudio.com/", "dev.azure.com/DevDiv/");
    chrome.tabs.update(tabs[0].id, { url: url } ); 
  });
}, { url: [{ hostSuffix: 'devdiv.visualstudio.com' }]});

chrome.pageAction.onClicked.addListener(onClicked);

// When the extension is installed or upgraded ...
chrome.runtime.onInstalled.addListener(function() {
  // Replace all rules ...
  chrome.declarativeContent.onPageChanged.removeRules(undefined, function() {
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
        actions: [ new chrome.declarativeContent.ShowPageAction() ]
      }
    ]);
  });
});