﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using WebFormsMvp.Binder;

namespace WebFormsMvp.Web
{
    /// <summary>
    /// Provides lifecycle management and synchronization services required for efficiently
    /// hosting multiple WebFormsMvp based controls on an ASP.NET page.
    /// </summary>
    public class PageViewHost
    {
        readonly PresenterBinder presenterBinder;

        /// <summary>
        /// Initializes a new instance of the <see cref="PageViewHost"/> class.
        /// </summary>
        /// <param name="page">The page instance that this view host will be responsible for.</param>
        /// <param name="httpContext">The owning HTTP context.</param>
        public PageViewHost(Page page, HttpContext httpContext)
        {
            if (page == null)
                throw new ArgumentNullException("page");

            var hosts = FindHosts(page).ToArray();

            presenterBinder = new PresenterBinder(hosts, httpContext);

            var asyncManager = new PageAsyncTaskManagerWrapper(page);
            presenterBinder.PresenterCreated += (sender, args) =>
            {
                args.Presenter.AsyncManager = asyncManager;
            };

            page.Init += Page_Init;
            page.PreRenderComplete += Page_PreRenderComplete;
            page.Unload += Page_Unload;
        }

        void RegisterView(IView view)
        {
            presenterBinder.RegisterView(view);
        }

        void Page_Init(object sender, EventArgs e)
        {
            presenterBinder.PerformBinding();
        }

        void Page_PreRenderComplete(object sender, EventArgs e)
        {
            presenterBinder.MessageCoordinator.Close();
        }

        void Page_Unload(object sender, EventArgs e)
        {
            presenterBinder.Release();
        }

        internal static IEnumerable<object> FindHosts(Page page)
        {
            yield return page;

            var masterHost = page.Master;
            while (masterHost != null)
            {
                yield return masterHost;
                masterHost = masterHost.Master;
            }
        }

        readonly static string viewHostCacheKey = typeof(PageViewHost).FullName + ".PageContextKey";
        internal static PageViewHost FindViewHost(Control control, HttpContext httpContext)
        {
            var pageContext = control.Page.Items;

            if (pageContext.Contains(viewHostCacheKey))
                return (PageViewHost)pageContext[viewHostCacheKey];

            var host = new PageViewHost(control.Page, httpContext);

            pageContext[viewHostCacheKey] = host;

            return host;
        }

        /// <summary>
        /// Registers the specified control into the view host for the current page.
        /// If no view host has yet been initialized for the current page instance, one will be created.
        /// </summary>
        /// <param name="control">The control instance to register.</param>
        /// <param name="httpContext">The owning HTTP context.</param>
        /// <exception cref="ArgumentNullException">The control argument was null.</exception>
        /// <exception cref="InvalidOperationException">The control is not in a valid state for registration. Controls can only be registered once they have been added to the live control tree. The best place to register them is within the control's <see cref="System.Web.UI.Control.Init"/> event.</exception>
        public static void Register<T>(T control, HttpContext httpContext)
            where T : Control, IView
        {
            if (control == null)
                throw new ArgumentNullException("control");
            
            if (control.Page == null)
                throw new InvalidOperationException("Controls can only be registered once they have been added to the live control tree. The best place to register them is within the control's Init event.");

            var viewHost = FindViewHost(control, httpContext);
            viewHost.RegisterView(control);
        }
    }
}