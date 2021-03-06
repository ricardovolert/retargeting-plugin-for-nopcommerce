﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Nop.Core;
using Nop.Core.Caching;
using Nop.Core.Domain.Catalog;
using Nop.Core.Domain.Customers;
using Nop.Core.Domain.Discounts;
using Nop.Core.Domain.Media;
using Nop.Core.Domain.Orders;
using Nop.Core.Domain.Tax;
using Nop.Core.Plugins;
using Nop.Plugin.Widgets.Retargeting.Infrastructure.Cache;
using Nop.Plugin.Widgets.Retargeting.Models;
using Nop.Services.Catalog;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Customers;
using Nop.Services.Discounts;
using Nop.Services.Localization;
using Nop.Services.Logging;
using Nop.Services.Media;
using Nop.Services.Orders;
using Nop.Services.Seo;
using Nop.Services.Stores;
using Nop.Services.Topics;
using Nop.Web.Framework.Controllers;
using Nop.Web.Framework.Mvc;
using Nop.Web.Framework.Security;
using Nop.Web.Framework.UI;
using OrderItem = Nop.Plugin.Widgets.Retargeting.Models.OrderItem;

namespace Nop.Plugin.Widgets.Retargeting.Controllers
{
    public class WidgetsRetargetingController : BasePluginController
    {
        private readonly ITopicService _topicService;
        private readonly IStoreService _storeService;
        private readonly IOrderService _orderService;
        private readonly IPictureService _pictureService;
        private readonly ISettingService _settingService;
        private readonly IProductService _productService;
        private readonly IDiscountService _discountService;
        private readonly ICategoryService _categoryService;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly IManufacturerService _manufacturerService;
        private readonly ILocalizationService _localizationService;
        private readonly IProductAttributeService _productAttributeService;

        private readonly ILogger _logger;
        private readonly IWorkContext _workContext;
        private readonly IPluginFinder _pluginFinder;
        private readonly IStoreContext _storeContext;
        private readonly ICacheManager _cacheManager;
        private readonly IProductAttributeParser _productAttributeParser;

        private readonly MediaSettings _mediaSettings;

        private readonly HttpContextBase _httpContext;

        public WidgetsRetargetingController(
            ITopicService topicService,
            IStoreService storeService,
            IOrderService orderService,
            IPictureService pictureService,
            ISettingService settingService,
            IProductService productService,
            ICategoryService categoryService,
            IDiscountService discountService,
            IShoppingCartService shoppingCartService,
            IManufacturerService manufacturerService,
            ILocalizationService localizationService,
            IProductAttributeService productAttributeService,

            ILogger logger,
            IWorkContext workContext,
            IPluginFinder pluginFinder,
            IStoreContext storeContext,
            ICacheManager cacheManager,
            IProductAttributeParser productAttributeParser,

            MediaSettings mediaSettings,

            HttpContextBase httpContext
            )
        {
            _topicService = topicService;
            _storeService = storeService;
            _orderService = orderService;
            _pictureService = pictureService;
            _settingService = settingService;
            _productService = productService;
            _categoryService = categoryService;
            _discountService = discountService;
            _shoppingCartService = shoppingCartService;
            _manufacturerService = manufacturerService;
            _localizationService = localizationService;
            _productAttributeService = productAttributeService;

            _logger = logger;
            _workContext = workContext;
            _pluginFinder = pluginFinder;
            _storeContext = storeContext;
            _cacheManager = cacheManager;
            _productAttributeParser = productAttributeParser;

            _mediaSettings = mediaSettings;

            _httpContext = httpContext;
        }

        [AdminAuthorize]
        [ChildActionOnly]
        public ActionResult Configure()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var retargetingSettings = _settingService.LoadSetting<RetargetingSettings>(storeScope);

            var model = new ConfigurationModel
            {
                TrackingApiKey = retargetingSettings.TrackingApiKey,
                RestApiKey = retargetingSettings.RestApiKey,
                UseHttpPostInsteadOfAjaxInAddToCart = retargetingSettings.UseHttpPostInsteadOfAjaxInAddToCart,
                AddToCartButtonIdDetailsPrefix = retargetingSettings.AddToCartButtonIdDetailsPrefix,
                ProductPriceLabelDetailsSelector = retargetingSettings.ProductPriceLabelDetailsSelector,
                AddToWishlistButtonIdDetailsPrefix = retargetingSettings.AddToWishlistButtonIdDetailsPrefix,
                HelpTopicSystemNames = retargetingSettings.HelpTopicSystemNames,
                AddToWishlistCatalogButtonSelector = retargetingSettings.AddToWishlistCatalogButtonSelector,
                ProductReviewAddedResultSelector = retargetingSettings.ProductReviewAddedResultSelector,
                AddToCartCatalogButtonSelector = retargetingSettings.AddToCartCatalogButtonSelector,
                ProductBoxSelector = retargetingSettings.ProductBoxSelector,
                ProductMainPictureIdDetailsPrefix = retargetingSettings.ProductMainPictureIdDetailsPrefix,

                ActiveStoreScopeConfiguration = storeScope
            };

            if (storeScope > 0)
            {
                model.TrackingApiKey_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.TrackingApiKey, storeScope);
                model.RestApiKey_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.RestApiKey, storeScope);
                model.UseHttpPostInsteadOfAjaxInAddToCart_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.UseHttpPostInsteadOfAjaxInAddToCart, storeScope);
                model.AddToCartButtonDetailsPrefix_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.AddToCartButtonIdDetailsPrefix, storeScope);
                model.PriceLabelSelector_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.ProductPriceLabelDetailsSelector, storeScope);
                model.AddToWishlistButtonIdDetailsPrefix_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.AddToWishlistButtonIdDetailsPrefix, storeScope);
                model.HelpTopicSystemNames_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.HelpTopicSystemNames, storeScope);
                model.AddToWishlistCatalogButtonSelector_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.AddToWishlistCatalogButtonSelector, storeScope);
                model.ProductReviewAddedResultSelector_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.ProductReviewAddedResultSelector, storeScope);
                model.AddToCartCatalogButtonSelector_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.AddToCartCatalogButtonSelector, storeScope);
                model.ProductBoxSelector_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.ProductBoxSelector, storeScope);
                model.ProductMainPictureIdDetailsPrefix_OverrideForStore = _settingService.SettingExists(retargetingSettings, x => x.ProductMainPictureIdDetailsPrefix, storeScope);
            }

            return View("~/Plugins/Widgets.Retargeting/Views/WidgetsRetargeting/Configure.cshtml", model);
        }

        [HttpPost]
        [AdminAuthorize]
        [ChildActionOnly]
        [FormValueRequired("save")]
        public ActionResult Configure(ConfigurationModel model)
        {
            if (!ModelState.IsValid)
                return Configure();

            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var retargetingSettings = _settingService.LoadSetting<RetargetingSettings>(storeScope);

            //save settings
            retargetingSettings.TrackingApiKey = model.TrackingApiKey;
            retargetingSettings.RestApiKey = model.RestApiKey;
            retargetingSettings.UseHttpPostInsteadOfAjaxInAddToCart = model.UseHttpPostInsteadOfAjaxInAddToCart;
            retargetingSettings.AddToCartButtonIdDetailsPrefix = model.AddToCartButtonIdDetailsPrefix;
            retargetingSettings.ProductPriceLabelDetailsSelector = model.ProductPriceLabelDetailsSelector;
            retargetingSettings.AddToWishlistButtonIdDetailsPrefix = model.AddToWishlistButtonIdDetailsPrefix;
            retargetingSettings.HelpTopicSystemNames = model.HelpTopicSystemNames;
            retargetingSettings.AddToWishlistCatalogButtonSelector = model.AddToWishlistCatalogButtonSelector;
            retargetingSettings.ProductReviewAddedResultSelector = model.ProductReviewAddedResultSelector;
            retargetingSettings.AddToCartCatalogButtonSelector = model.AddToCartCatalogButtonSelector;
            retargetingSettings.ProductBoxSelector = model.ProductBoxSelector;
            retargetingSettings.ProductMainPictureIdDetailsPrefix = model.ProductMainPictureIdDetailsPrefix;

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.TrackingApiKey, model.TrackingApiKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.RestApiKey, model.RestApiKey_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.UseHttpPostInsteadOfAjaxInAddToCart, model.UseHttpPostInsteadOfAjaxInAddToCart_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToCartButtonIdDetailsPrefix, model.AddToCartButtonDetailsPrefix_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductPriceLabelDetailsSelector, model.PriceLabelSelector_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToWishlistButtonIdDetailsPrefix, model.AddToWishlistButtonIdDetailsPrefix_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.HelpTopicSystemNames, model.HelpTopicSystemNames_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToWishlistCatalogButtonSelector, model.AddToWishlistCatalogButtonSelector_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductReviewAddedResultSelector, model.ProductReviewAddedResultSelector_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToCartCatalogButtonSelector, model.AddToCartCatalogButtonSelector_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductBoxSelector, model.ProductBoxSelector_OverrideForStore, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductMainPictureIdDetailsPrefix, model.ProductMainPictureIdDetailsPrefix_OverrideForStore, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Admin.Plugins.Saved"));

            return Configure();
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("preconfigure")]
        public ActionResult Preconfigure()
        {
            var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("Widgets.Retargeting");
            if (pluginDescriptor == null)
                throw new Exception("Cannot load the plugin");

            var plugin = pluginDescriptor.Instance() as RetargetingPlugin;
            if (plugin == null)
                throw new Exception("Cannot load the plugin");

            try
            {
                plugin.Preconfigure();
                var message = _localizationService.GetResource("Plugins.Widgets.Retargeting.PreconfigureCompleted");
                AddNotification(NotifyType.Success, message, true);
                _logger.Information(message);
            }
            catch (Exception exception)
            {
                var message = _localizationService.GetResource("Plugins.Widgets.Retargeting.PreconfigureError") + exception;
                AddNotification(NotifyType.Error, message, true);
                _logger.Error(message);
            }

            return Configure();
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("reset-settings")]
        public ActionResult ResetSettings()
        {
            //load settings for a chosen store scope
            var storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _workContext);
            var retargetingSettings = _settingService.LoadSetting<RetargetingSettings>(storeScope);

            //save settings
            retargetingSettings.UseHttpPostInsteadOfAjaxInAddToCart = false;
            retargetingSettings.AddToCartButtonIdDetailsPrefix = "add-to-cart-button-";
            retargetingSettings.ProductPriceLabelDetailsSelector = ".prices";
            retargetingSettings.AddToWishlistButtonIdDetailsPrefix = "add-to-wishlist-button-";
            retargetingSettings.HelpTopicSystemNames = "ShippingInfo,PrivacyInfo,ConditionsOfUse,AboutUs";
            retargetingSettings.AddToWishlistCatalogButtonSelector = ".add-to-wishlist-button";
            retargetingSettings.ProductReviewAddedResultSelector = "div.result";
            retargetingSettings.AddToCartCatalogButtonSelector = ".product-box-add-to-cart-button";
            retargetingSettings.ProductBoxSelector = ".product-item";
            retargetingSettings.ProductMainPictureIdDetailsPrefix = "main-product-img-lightbox-anchor-";

            /* We do not clear cache after each setting update.
             * This behavior can increase performance because cached settings will not be cleared 
             * and loaded from database after each update */
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.UseHttpPostInsteadOfAjaxInAddToCart, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToCartButtonIdDetailsPrefix, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductPriceLabelDetailsSelector, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToWishlistButtonIdDetailsPrefix, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.HelpTopicSystemNames, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToWishlistCatalogButtonSelector, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductReviewAddedResultSelector, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.AddToCartCatalogButtonSelector, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductBoxSelector, false, storeScope, false);
            _settingService.SaveSettingOverridablePerStore(retargetingSettings, x => x.ProductMainPictureIdDetailsPrefix, false, storeScope, false);

            //now clear settings cache
            _settingService.ClearCache();

            SuccessNotification(_localizationService.GetResource("Plugins.Widgets.Retargeting.SettingsReset"));

            return Configure();
        }

        [ChildActionOnly]
        public ActionResult PublicInfo(string widgetZone, object additionalData = null)
        {
            var retargetingSettings = _settingService.LoadSetting<RetargetingSettings>(_storeContext.CurrentStore.Id);

            var model = new PublicInfoModel
            {
                TrackingApiKey = retargetingSettings.TrackingApiKey,
                AddToCartButtonIdDetailsPrefix = retargetingSettings.AddToCartButtonIdDetailsPrefix,
                ProductPriceLabelDetailsSelector = retargetingSettings.ProductPriceLabelDetailsSelector,
                AddToWishlistButtonIdDetailsPrefix = retargetingSettings.AddToWishlistButtonIdDetailsPrefix,
                AddToWishlistCatalogButtonSelector = retargetingSettings.AddToWishlistCatalogButtonSelector,
                ProductReviewAddedResultSelector = retargetingSettings.ProductReviewAddedResultSelector,
                AddToCartCatalogButtonSelector = retargetingSettings.AddToCartCatalogButtonSelector,
                ProductBoxSelector = retargetingSettings.ProductBoxSelector,
                ProductMainPictureIdDetailsPrefix = retargetingSettings.ProductMainPictureIdDetailsPrefix,

                RenderAddToCartFunc = true,
                RenderAddToWishlistFunc = true
            };

            var routeData = ((System.Web.UI.Page)this.HttpContext.CurrentHandler).RouteData;
            var controllerName = routeData.Values["controller"];
            var actionName = routeData.Values["action"];

            //user info
            var customer = _httpContext.Session != null ? _httpContext.Session["ra_Customer"] as Customer : null;
            if (customer != null)
            {
                model.RenderSetEmailFunc = true;
                model.CustomerModel = new CustomerModel()
                {
                    Name = HttpUtility.JavaScriptStringEncode(
                           customer.GetAttribute<string>(SystemCustomerAttributeNames.FirstName) + " " +
                           customer.GetAttribute<string>(SystemCustomerAttributeNames.LastName)),
                    Email = customer.Email,
                    City = HttpUtility.JavaScriptStringEncode(customer.GetAttribute<string>(SystemCustomerAttributeNames.City)),
                    Phone = HttpUtility.JavaScriptStringEncode(customer.GetAttribute<string>(SystemCustomerAttributeNames.Phone))
                };

                var gender = customer.GetAttribute<string>(SystemCustomerAttributeNames.Gender);
                switch (gender)
                {
                    case "M":
                        model.CustomerModel.Sex = "1";
                        break;
                    case "F":
                        model.CustomerModel.Sex = "0";
                        break;
                    default:
                        model.CustomerModel.Sex = "";
                        break;
                }

                var dateOfBirth = customer.GetAttribute<DateTime?>(SystemCustomerAttributeNames.DateOfBirth);
                if (dateOfBirth.HasValue)
                    model.CustomerModel.Birthday = dateOfBirth.Value.ToString("dd-MM-yyyy");

                _httpContext.Session["ra_Customer"] = null;
            }

            //category page
            if (controllerName.ToString().Equals("catalog", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("category", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderSendCategoryFunc = true;

                int categoryId;
                if (routeData.Values.ContainsKey("categoryid") && int.TryParse(routeData.Values["categoryid"].ToString(), out categoryId) && categoryId > 0)
                {
                    var category = _categoryService.GetCategoryById(categoryId);
                    if (category != null)
                    {
                        var categoryModel = new CategoryModel
                        {
                            CategoryId = category.Id,
                            CategoryName = HttpUtility.JavaScriptStringEncode(category.Name)
                        };

                        var parentCategory = _categoryService.GetCategoryById(category.ParentCategoryId);
                        if (parentCategory != null)
                        {
                            categoryModel.ParentCategoryId = parentCategory.Id;
                            categoryModel.ParentCategoryName = HttpUtility.JavaScriptStringEncode(parentCategory.Name);

                            var parentParentCategory = _categoryService.GetCategoryById(parentCategory.ParentCategoryId);
                            if (parentParentCategory != null)
                            {
                                categoryModel.ParentParentCategoryId = parentParentCategory.Id;
                                categoryModel.ParentParentCategoryName = HttpUtility.JavaScriptStringEncode(parentParentCategory.Name);
                            }
                        }

                        model.CategoryModel = categoryModel;
                    }
                }
            }

            //manufacturer page
            if (controllerName.ToString().Equals("catalog", StringComparison.InvariantCultureIgnoreCase) &&
                    actionName.ToString().Equals("manufacturer", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderSendBrandFunc = true;

                int manufacturerId;
                if (routeData.Values.ContainsKey("manufacturerid") &&
                    int.TryParse(routeData.Values["manufacturerid"].ToString(), out manufacturerId) &&
                    manufacturerId > 0)
                {
                    var manufacturer = _manufacturerService.GetManufacturerById(manufacturerId);
                    if (manufacturer != null)
                    {
                        var manufacturerModel = new ManufacturerModel
                        {
                            ManufacturerId = manufacturer.Id,
                            ManufacturerName = HttpUtility.JavaScriptStringEncode(manufacturer.Name)
                        };

                        model.ManufacturerModel = manufacturerModel;
                    }
                }
            }

            //product page
            if (controllerName.ToString().Equals("product", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("productdetails", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderSendProductFunc = true;

                int productId;
                if (routeData.Values.ContainsKey("productid") &&
                    int.TryParse(routeData.Values["productid"].ToString(), out productId) &&
                    productId > 0)
                {
                    var product = _productService.GetProductById(productId);
                    if (product != null)
                        model.ProductId = product.Id;
                }

                if (_mediaSettings.DefaultPictureZoomEnabled)
                    model.RenderClickImageFunc = true;
            }

            //order completed page
            if (controllerName.ToString().Equals("checkout", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("completed", StringComparison.InvariantCultureIgnoreCase))
            {
                int orderId;
                Order order;
                if (routeData.Values.ContainsKey("orderid") &&
                    int.TryParse(routeData.Values["orderid"].ToString(), out orderId) &&
                    orderId > 0)
                {
                    order = _orderService.GetOrderById(orderId);
                }
                else
                {
                    order = _orderService.SearchOrders(_storeContext.CurrentStore.Id,
                        customerId: _workContext.CurrentCustomer.Id, pageSize: 1)
                        .FirstOrDefault();
                }

                if (order != null && !order.Deleted && _workContext.CurrentCustomer.Id == order.CustomerId)
                {
                    model.RenderSendOrderFunc = true;
                    var orderModel = new OrderModel
                    {
                        Id = order.Id,
                        City = HttpUtility.JavaScriptStringEncode(order.BillingAddress.City),
                        Discount = order.OrderDiscount,
                        Email = order.BillingAddress.Email,
                        FirstName = HttpUtility.JavaScriptStringEncode(order.BillingAddress.FirstName),
                        LastName = HttpUtility.JavaScriptStringEncode(order.BillingAddress.LastName),
                        Phone = HttpUtility.JavaScriptStringEncode(order.BillingAddress.PhoneNumber),
                        State =
                            order.BillingAddress.StateProvince != null
                                ? HttpUtility.JavaScriptStringEncode(order.BillingAddress.StateProvince.Name)
                                : "",
                        Address =
                            HttpUtility.JavaScriptStringEncode(order.BillingAddress.Address1 + " " +
                                                               order.BillingAddress.Address2),
                        Shipping =
                            order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax
                                ? order.OrderShippingInclTax
                                : order.OrderShippingExclTax,
                        Total = order.OrderTotal,
                        Rebates = 0,
                        Fees = 0
                    };

                    //discount codes
                    var discountsWithCouponCodes =
                        _discountService.GetAllDiscountUsageHistory(orderId: order.Id)
                            .Where(x => !string.IsNullOrEmpty(x.Discount.CouponCode))
                            .ToList();
                    for (var i = 0; i < discountsWithCouponCodes.Count; i++)
                    {
                        orderModel.DiscountCode += discountsWithCouponCodes[i].Discount.CouponCode;

                        if (i < discountsWithCouponCodes.Count - 1)
                            orderModel.DiscountCode += ", ";
                    }

                    var dateOfBirth = order.Customer.GetAttribute<DateTime?>(SystemCustomerAttributeNames.DateOfBirth);
                    if (dateOfBirth.HasValue)
                        orderModel.Birthday = dateOfBirth.Value.ToString("dd-mm-yyyy");

                    //order items
                    foreach (var orderItem in order.OrderItems)
                    {
                        var item = new OrderItem()
                        {
                            Id = orderItem.ProductId,
                            Quantity = orderItem.Quantity,
                            Price = order.CustomerTaxDisplayType == TaxDisplayType.IncludingTax
                                ? orderItem.UnitPriceInclTax
                                : orderItem.UnitPriceExclTax,
                        };

                        var values = _productAttributeParser.ParseProductAttributeValues(orderItem.AttributesXml);
                        for (var i = 0; i < values.Count; i++)
                        {
                            item.VariationCode += values[i].Id;
                            if (i < values.Count - 1)
                                item.VariationCode += "-";
                        }

                        orderModel.OrderItems.Add(item);
                    }

                    model.OrderModel = orderModel;
                }
            }

            //product review added page
            if (controllerName.ToString().Equals("product", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("productreviews", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderCommentOnProductFunc = true;

                int productId;
                if (routeData.Values.ContainsKey("productid") &&
                    int.TryParse(routeData.Values["productid"].ToString(), out productId))
                {
                    model.ProductId = productId;
                }
            }

            //help pages
            if (controllerName.ToString().Equals("topic", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("topicdetails", StringComparison.InvariantCultureIgnoreCase))
            {
                int topicId;
                if (routeData.Values.ContainsKey("topicid") &&
                    int.TryParse(routeData.Values["topicid"].ToString(), out topicId))
                {
                    var systemNames = retargetingSettings.HelpTopicSystemNames.Split(Convert.ToChar(",")).Select(s => s.Trim()).ToList();
                    var topic = _topicService.GetTopicById(topicId);
                    if (topic != null && systemNames.Contains(topic.SystemName))
                    {
                        model.RenderVisitHelpPageFunc = true;
                    }
                }
            }

            //checkout page
            if (controllerName.ToString().Equals("shoppingcart", StringComparison.InvariantCultureIgnoreCase) &&
                actionName.ToString().Equals("cart", StringComparison.InvariantCultureIgnoreCase))
            {
                model.RenderCheckoutIdsFunc = true;

                var cart = _workContext.CurrentCustomer.ShoppingCartItems
                    .Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart)
                    .LimitPerStore(_storeContext.CurrentStore.Id)
                    .ToList();

                foreach (var cartItem in cart)
                {
                    model.CartItemIds += cartItem.ProductId;
                    model.CartItemIds += ",";
                }
            }

            //remove from cart
            model.CartItemsToDelete = _httpContext.Session != null
                ? _httpContext.Session["ra_shoppingCartItemsToDelete"] as
                    Dictionary<int, Dictionary<string, string>> ?? new Dictionary<int, Dictionary<string, string>>()
                : new Dictionary<int, Dictionary<string, string>>();
            _httpContext.Session["ra_shoppingCartItemsToDelete"] = null;

            //add to cart
            if (retargetingSettings.UseHttpPostInsteadOfAjaxInAddToCart)
            {
                model.AddToCartProductInfo = _httpContext.Session != null
                    ? _httpContext.Session["ra_addToCartProductInfo"]
                    : null;
                _httpContext.Session["ra_addToCartProductInfo"] = null;
            }

            return View("~/Plugins/Widgets.Retargeting/Views/WidgetsRetargeting/PublicInfo.cshtml", model);
        }

        [NopHttpsRequirement(SslRequirement.No)]
        public ActionResult ProductStockFeed()
        {
            var productFeed = new object();
            try
            {
                var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("Widgets.Retargeting");
                if (pluginDescriptor == null)
                    throw new Exception("Cannot load the plugin");

                var plugin = pluginDescriptor.Instance() as RetargetingPlugin;
                if (plugin == null)
                    throw new Exception("Cannot load the plugin");

                productFeed = plugin.GenerateProductStockFeed();
            }
            catch (Exception exc)
            {
                ErrorNotification(exc.Message);
                _logger.Error(exc.Message, exc);
            }

            return Json(productFeed, JsonRequestBehavior.AllowGet);
        }

        [NopHttpsRequirement(SslRequirement.No)]
        public ActionResult GenerateDiscounts(string key, string value, int type, int count)
        {
            var discountCodes = new List<string>();

            var retargetingSettings = _settingService.LoadSetting<RetargetingSettings>(_storeContext.CurrentStore.Id);
            if (retargetingSettings.RestApiKey.Equals(key))
            {
                decimal discountValue;
                decimal.TryParse(value, out discountValue);

                if (discountValue > 0)
                {
                    for (var i = 0; i < count; i++)
                    {
                        var discount = new Discount
                        {
                            RequiresCouponCode = true,
                            CouponCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper()
                        };

                        switch (type)
                        {
                            case 0:
                                discount.Name = string.Format("Retargeting_{0}_{1}_discount", value, "AMOUNT");
                                discount.DiscountType = DiscountType.AssignedToOrderTotal;
                                discount.DiscountAmount = discountValue;
                                break;

                            case 1:
                                discount.Name = string.Format("Retargeting_{0}_{1}_discount", value, "PERCENTAGE");
                                discount.DiscountType = DiscountType.AssignedToOrderTotal;
                                discount.UsePercentage = true;
                                discount.DiscountPercentage = discountValue;
                                break;

                            case 2:
                            default:
                                discount.Name = string.Format("Retargeting_{0}_discount", "FREE_SHIPPING");
                                discount.DiscountType = DiscountType.AssignedToShipping;
                                discount.UsePercentage = true;
                                discount.DiscountPercentage = 100;
                                break;
                        }
                        _discountService.InsertDiscount(discount);
                        discountCodes.Add(discount.CouponCode);
                    }
                }
            }

            return Json(discountCodes.ToArray(), JsonRequestBehavior.AllowGet);
        }

        [NonAction]
        private string GetProductImageUrl(Product product)
        {
            var pictures = _pictureService.GetPicturesByProductId(product.Id);
            var defaultPicture = pictures.FirstOrDefault();

            return _pictureService.GetPictureUrl(defaultPicture);
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult GetProductInfo(int productId)
        {
            var productInfo = new Dictionary<string, object>();

            try
            {
                var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("Widgets.Retargeting");
                if (pluginDescriptor == null)
                    throw new Exception("Cannot load the plugin");

                var plugin = pluginDescriptor.Instance() as RetargetingPlugin;
                if (plugin == null)
                    throw new Exception("Cannot load the plugin");

                var product = _productService.GetProductById(productId);
                if (product == null)
                    return new NullJsonResult();

                #region Product details

                productInfo.Add("id", product.Id);
                productInfo.Add("name", HttpUtility.JavaScriptStringEncode(product.GetLocalized(x => x.Name)));
                productInfo.Add("url", string.Format("{0}{1}", _storeContext.CurrentStore.Url, product.GetSeName()));
                productInfo.Add("img", GetProductImageUrl(product));

                decimal price;
                decimal priceWithDiscount;
                plugin.GetProductPrice(product, out price, out priceWithDiscount);

                if (price == 0)
                    price = 1;

                productInfo.Add("price", price.ToString(new CultureInfo("en-US", false).NumberFormat));
                productInfo.Add("promo", priceWithDiscount.ToString(new CultureInfo("en-US", false).NumberFormat));

                #endregion

                #region Categories

                var categoriesCacheKey =
                    string.Format(ModelCacheEventConsumer.PRODUCT_CATEGORIES_MODEL_KEY,
                        product.Id,
                        _workContext.WorkingLanguage.Id,
                        string.Join(",", _workContext.CurrentCustomer.GetCustomerRoleIds()),
                        _storeContext.CurrentStore.Id);

                var categories = _cacheManager.Get(categoriesCacheKey, () =>
                {
                    return _categoryService.GetProductCategoriesByProductId(product.Id)
                        .Select(x => x.Category)
                        .ToList();
                });

                if (categories.Count == 0)
                {
                    if (product.ParentGroupedProductId > 0)
                    {
                        categoriesCacheKey =
                            string.Format(ModelCacheEventConsumer.PRODUCT_CATEGORIES_MODEL_KEY,
                                product.ParentGroupedProductId,
                                _workContext.WorkingLanguage.Id,
                                string.Join(",", _workContext.CurrentCustomer.GetCustomerRoleIds()),
                                _storeContext.CurrentStore.Id);

                        categories = _cacheManager.Get(categoriesCacheKey, () =>
                        {
                            return _categoryService.GetProductCategoriesByProductId(product.ParentGroupedProductId)
                                .Select(x => x.Category)
                                .ToList();
                        });
                    }
                }

                //product must have at least one category
                if (categories.Count == 0)
                    categories.Add(new Category { Name = "Default category" });

                var categoriesResult = new List<object>();
                foreach (var category in categories)
                {
                    var categoryObj = new Dictionary<string, object>
                {
                    {"name", HttpUtility.JavaScriptStringEncode(category.GetLocalized(x => x.Name))},
                    {"id", category.Id}
                };

                    var breadcrumb = new List<object>();
                    if (category.ParentCategoryId > 0)
                    {
                        categoryObj.Add("parent", category.ParentCategoryId);

                        var parentCategory = _categoryService.GetCategoryById(category.ParentCategoryId);
                        if (parentCategory != null)
                        {
                            var bc1 = new Dictionary<string, object>
                        {
                            {"id", parentCategory.Id},
                            {"name", HttpUtility.JavaScriptStringEncode(parentCategory.GetLocalized(x => x.Name))}
                        };

                            if (parentCategory.ParentCategoryId > 0)
                                bc1.Add("parent", parentCategory.ParentCategoryId);
                            else
                                bc1.Add("parent", false);

                            breadcrumb.Add(bc1);

                            var parentParentCategory = _categoryService.GetCategoryById(parentCategory.ParentCategoryId);
                            if (parentParentCategory != null)
                            {
                                breadcrumb.Add(new Dictionary<string, object>
                            {
                                {"id", parentParentCategory.Id},
                                {"name", HttpUtility.JavaScriptStringEncode(parentParentCategory.GetLocalized(x => x.Name))},
                                {"parent", false}
                            });
                            }
                        }
                    }
                    else
                    {
                        categoryObj.Add("parent", false);
                    }

                    categoryObj.Add("breadcrumb", breadcrumb);
                    categoriesResult.Add(categoryObj);
                }
                productInfo.Add("category", categoriesResult);

                #endregion

                #region Manufacturer

                var manufacturer = new Dictionary<string, object>();

                var manufacturersCacheKey =
                    string.Format(ModelCacheEventConsumer.PRODUCT_MANUFACTURERS_MODEL_KEY,
                        productId,
                        _workContext.WorkingLanguage.Id,
                        string.Join(",", _workContext.CurrentCustomer.GetCustomerRoleIds()),
                        _storeContext.CurrentStore.Id);

                var manufacturers = _cacheManager.Get(manufacturersCacheKey, () =>
                {
                    return _manufacturerService.GetProductManufacturersByProductId(productId)
                        .Select(x => x.Manufacturer)
                        .ToList();
                });

                if (manufacturers.Count > 0)
                {
                    manufacturer.Add("id", manufacturers[0].Id);
                    manufacturer.Add("name", HttpUtility.JavaScriptStringEncode(manufacturers[0].GetLocalized(m => m.Name)));

                    productInfo.Add("brand", manufacturer);
                }
                else
                {
                    productInfo.Add("brand", false);
                }

                #endregion

                #region Inventory

                var inventory = new Dictionary<string, object>();
                var attributes = new Dictionary<string, object>();

                var allAttributeCombinationsXml = _productAttributeParser.GenerateAllCombinations(product, true);
                foreach (var attributeCombinationXml in allAttributeCombinationsXml)
                {
                    var warnings = new List<string>();
                    warnings.AddRange(_shoppingCartService.GetShoppingCartItemAttributeWarnings(_workContext.CurrentCustomer,
                        ShoppingCartType.ShoppingCart, product, 1, attributeCombinationXml, true));
                    if (warnings.Count != 0)
                        continue;

                    var inStock = false;

                    switch (product.ManageInventoryMethod)
                    {
                        case ManageInventoryMethod.ManageStock:
                            {
                                #region Manage stock

                                if (!product.DisplayStockAvailability)
                                    inStock = true;

                                var stockQuantity = product.GetTotalStockQuantity();
                                if (stockQuantity > 0)
                                    inStock = true;
                                else
                                    //out of stock
                                    switch (product.BackorderMode)
                                    {
                                        case BackorderMode.AllowQtyBelow0:
                                            inStock = true;
                                            break;
                                        case BackorderMode.AllowQtyBelow0AndNotifyCustomer:
                                            inStock = true;
                                            break;
                                        case BackorderMode.NoBackorders:
                                        default:
                                            break;
                                    }

                                #endregion
                            }
                            break;

                        case ManageInventoryMethod.ManageStockByAttributes:
                            {
                                #region Manage stock by attributes

                                if (!product.DisplayStockAvailability)
                                    inStock = true;

                                var combination = _productAttributeParser.FindProductAttributeCombination(product, attributeCombinationXml);
                                if (combination != null)
                                {
                                    //combination exists
                                    var stockQuantity = combination.StockQuantity;
                                    if (stockQuantity > 0)
                                        inStock = true;
                                    else if (combination.AllowOutOfStockOrders)
                                        inStock = true;
                                }
                                else
                                {
                                    //no combination configured
                                    if (!product.AllowAddingOnlyExistingAttributeCombinations)
                                        inStock = true;
                                }

                                #endregion
                            }
                            break;
                        case ManageInventoryMethod.DontManageStock:
                        default:
                            inStock = true;
                            break;
                    }

                    var varCode = plugin.GetCombinationCode(attributeCombinationXml);
                    if (!attributes.ContainsKey(varCode))
                        attributes.Add(varCode, inStock);
                }
                if (attributes.Count > 0)
                {
                    inventory.Add("variations", true);
                    inventory.Add("stock", attributes);
                }
                else
                {
                    inventory.Add("variations", false);
                    inventory.Add("stock", product.StockQuantity > 0);
                }

                productInfo.Add("inventory", inventory);

                #endregion
            }
            catch (Exception exc)
            {
                ErrorNotification(exc.Message);
                _logger.Error(exc.Message, exc);
            }

            return Json(new
            {
                result = productInfo
            });
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult IsProductCombinationInStock(int productId, FormCollection form)
        {
            var product = _productService.GetProductById(productId);
            if (product == null)
                return new NullJsonResult();

            var pluginDescriptor = _pluginFinder.GetPluginDescriptorBySystemName("Widgets.Retargeting");
            if (pluginDescriptor == null)
                throw new Exception("Cannot load the plugin");

            var plugin = pluginDescriptor.Instance() as RetargetingPlugin;
            if (plugin == null)
                throw new Exception("Cannot load the plugin");

            var attributeXml = ParseProductAttributes(product, form);

            string variationCode;
            Dictionary<string, object> variationDetails;

            var productIsInStock = plugin.IsProductCombinationInStock(product, attributeXml, out variationCode, out variationDetails);

            return Json(new
            {
                stock = productIsInStock,
                variationCode = variationCode,
                variationDetails = variationDetails
            });
        }

        [NonAction]
        private string ParseProductAttributes(Product product, FormCollection form)
        {
            string attributesXml = "";

            #region Product attributes

            var productAttributes = _productAttributeService.GetProductAttributeMappingsByProductId(product.Id);
            foreach (var attribute in productAttributes)
            {
                string controlId = string.Format("product_attribute_{0}", attribute.Id);
                switch (attribute.AttributeControlType)
                {
                    case AttributeControlType.DropdownList:
                    case AttributeControlType.RadioList:
                    case AttributeControlType.ColorSquares:
                    case AttributeControlType.ImageSquares:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!string.IsNullOrEmpty(ctrlAttributes))
                            {
                                int selectedAttributeId = int.Parse(ctrlAttributes);
                                if (selectedAttributeId > 0)
                                    attributesXml = _productAttributeParser.AddProductAttribute(attributesXml,
                                        attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    case AttributeControlType.Checkboxes:
                        {
                            var ctrlAttributes = form[controlId];
                            if (!String.IsNullOrEmpty(ctrlAttributes))
                            {
                                foreach (var item in ctrlAttributes.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                                {
                                    int selectedAttributeId = int.Parse(item);
                                    if (selectedAttributeId > 0)
                                        attributesXml = _productAttributeParser.AddProductAttribute(attributesXml,
                                            attribute, selectedAttributeId.ToString());
                                }
                            }
                        }
                        break;
                    case AttributeControlType.ReadonlyCheckboxes:
                        {
                            //load read-only (already server-side selected) values
                            var attributeValues = _productAttributeService.GetProductAttributeValues(attribute.Id);
                            foreach (var selectedAttributeId in attributeValues
                                .Where(v => v.IsPreSelected)
                                .Select(v => v.Id)
                                .ToList())
                            {
                                attributesXml = _productAttributeParser.AddProductAttribute(attributesXml,
                                    attribute, selectedAttributeId.ToString());
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            //validate conditional attributes (if specified)
            foreach (var attribute in productAttributes)
            {
                var conditionMet = _productAttributeParser.IsConditionMet(attribute, attributesXml);
                if (conditionMet.HasValue && !conditionMet.Value)
                {
                    attributesXml = _productAttributeParser.RemoveProductAttribute(attributesXml, attribute);
                }
            }

            #endregion

            return attributesXml;
        }
    }
}