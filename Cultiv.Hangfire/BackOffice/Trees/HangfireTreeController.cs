using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Trees;
using Umbraco.Cms.Web.BackOffice.Trees;
using Umbraco.Cms.Web.Common.Attributes;
using Umbraco.Cms.Web.Common.ModelBinders;

namespace Cultiv.Hangfire.BackOffice.Trees
{
    [PluginController("Cultiv.Hangfire")]
    [Tree(Constants.Applications.Hangfire.Alias, Constants.Trees.Hangfire.Alias, TreeTitle = Constants.Trees.Hangfire.Name, SortOrder = 1, IsSingleNodeTree = true)]
    public class HangfireTreeController : TreeController
    {
        private readonly IMenuItemCollectionFactory _menuItemCollectionFactory;

        public HangfireTreeController(
            ILocalizedTextService localizedTextService,
            UmbracoApiControllerTypeCollection umbracoApiControllerTypeCollection,
            IMenuItemCollectionFactory menuItemCollectionFactory,
            IEventAggregator eventAggregator)
            : base(localizedTextService, umbracoApiControllerTypeCollection, eventAggregator)
        {
            _menuItemCollectionFactory = menuItemCollectionFactory ?? throw new ArgumentNullException(nameof(menuItemCollectionFactory));
        }

        protected override ActionResult<TreeNodeCollection> GetTreeNodes(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {
            // Full screen app without tree nodes
            return TreeNodeCollection.Empty;
        }

        protected override ActionResult<MenuItemCollection> GetMenuForNode(string id, [ModelBinder(typeof(HttpQueryStringModelBinder))] FormCollection queryStrings)
        {
            return _menuItemCollectionFactory.Create();
        }

        protected override ActionResult<TreeNode> CreateRootNode(FormCollection queryStrings)
        {
            var rootResult = base.CreateRootNode(queryStrings);

            if (rootResult.Result is not null)
            {
                return rootResult;
            }

            var root = rootResult.Value;

            // This will load in a custom UI instead of the dashboard for the root node
            root.RoutePath = $"{Constants.Applications.Hangfire.Alias}/{Constants.Trees.Hangfire.Alias}/overview";
            root.HasChildren = false;

            return root;
        }
    }
}
