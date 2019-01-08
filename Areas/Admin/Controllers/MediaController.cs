﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bonsai.Areas.Admin.Logic;
using Bonsai.Areas.Admin.ViewModels.Media;
using Bonsai.Areas.Front.Logic;
using Bonsai.Code.DomainModel.Media;
using Bonsai.Code.Utils.Helpers;
using Bonsai.Code.Utils.Validation;
using Bonsai.Data;
using Impworks.Utils.Strings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;

namespace Bonsai.Areas.Admin.Controllers
{
    /// <summary>
    /// Controller for managing media files.
    /// </summary>
    [Route("admin/media")]
    public class MediaController: AdminControllerBase
    {
        public MediaController(MediaManagerService media, PagesManagerService pages, AppDbContext db)
        {
            _media = media;
            _pages = pages;
            _db = db;
        }

        private readonly MediaManagerService _media;
        private readonly PagesManagerService _pages;
        private readonly AppDbContext _db;

        /// <summary>
        /// Displays the list of pages.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> Index(MediaListRequestVM request)
        {
            var vm = await _media.GetMediaAsync(request);
            return View(vm);
        }

        /// <summary>
        /// Displays the uploader form.
        /// </summary>
        [HttpGet]
        [Route("upload")]
        public async Task<ActionResult> Upload()
        {
            return View();
        }

        /// <summary>
        /// Handles a single file upload.
        /// </summary>
        [HttpPost]
        [Route("upload")]
        public async Task<ActionResult> Upload(MediaUploadRequestVM vm, IFormFile file)
        {
            try
            {
                var result = await _media.UploadAsync(vm, file, User);
                result.ThumbnailPath = Url.Content(result.ThumbnailPath);

                await _db.SaveChangesAsync();

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    Error = true,
                    Description = ex.Message
                });
            }
        }

        /// <summary>
        /// Returns the thumbnail status for all specified media files.
        /// </summary>
        [HttpGet]
        [Route("thumbs")]
        public async Task<ActionResult> GetThumbnails(IEnumerable<Guid> ids)
        {
            var vms = await _media.GetThumbnailsAsync(ids);

            foreach(var vm in vms)
                vm.ThumbnailPath = Url.Content(vm.ThumbnailPath);

            return Json(vms);
        }

        /// <summary>
        /// Displays the update form for a media file.
        /// </summary>
        [HttpGet]
        [Route("update")]
        public async Task<ActionResult> Update(Guid id)
        {
            var vm = await _media.RequestUpdateAsync(id);
            return await ViewEditorFormAsync(vm);
        }

        /// <summary>
        /// Updates the media data.
        /// </summary>
        [HttpPost]
        [Route("update")]
        public async Task<ActionResult> Update(MediaEditorVM vm)
        {
            if(!ModelState.IsValid)
                return await ViewEditorFormAsync(vm);

            try
            {
                await _media.UpdateAsync(vm, User);
                await _db.SaveChangesAsync();

                return RedirectToSuccess("Медиа-файл обновлен");
            }
            catch (ValidationException ex)
            {
                SetModelState(ex);
                return await ViewEditorFormAsync(vm);
            }
        }

        /// <summary>
        /// Removes the media file.
        /// </summary>
        [HttpGet]
        [Route("remove")]
        public async Task<ActionResult> Remove(Guid id)
        {
            var vm = await _media.RequestRemoveAsync(id);
            return View(vm);
        }

        /// <summary>
        /// Removes the media file.
        /// </summary>
        [HttpPost]
        [Route("remove")]
        public async Task<ActionResult> Remove(Guid id, bool confirm)
        {
            await _media.RemoveAsync(id, User);
            await _db.SaveChangesAsync();

            return RedirectToSuccess("Медиа-файл удален");
        }

        #region Helpers

        /// <summary>
        /// Displays the editor.
        /// </summary>
        private async Task<ActionResult> ViewEditorFormAsync(MediaEditorVM vm)
        {
            var depictedEntities = JsonConvert.DeserializeObject<MediaTagVM[]>(vm.DepictedEntities);
            var ids = depictedEntities.Select(x => x.PageId)
                                      .Concat(new[] {vm.Location, vm.Event}.Select(x => x.TryParse<Guid?>()))
                                      .ToList();

            var pageLookup = await _pages.FindPagesByIdsAsync(ids);

            foreach(var depicted in depictedEntities)
                if (depicted.PageId != null && pageLookup.TryGetValue(depicted.PageId.Value, out var depPage))
                    depicted.ObjectTitle = depPage.Title;

            ViewBag.Data = new MediaEditorDataVM
            {
                EventItem = GetPageLookup(vm.Event),
                LocationItem = GetPageLookup(vm.Location),
                DepictedEntityItems = GetDepictedEntitiesList(),
                SaveActions = ViewHelper.GetEnumSelectList(vm.SaveAction),
                ThumbnailUrl = MediaPresenterService.GetSizedMediaPath(vm.FilePath, MediaSize.Large)
            };

            return View("Editor", vm);

            SelectListItem[] GetPageLookup(string key)
            {
                if (key.TryParse<Guid?>() is Guid id)
                    return pageLookup.TryGetValue(id, out var page)
                        ? new[] {new SelectListItem {Selected = true, Text = page.Title, Value = page.Id.ToString()}}
                        : Array.Empty<SelectListItem>();

                return new[] {new SelectListItem {Selected = true, Text = key, Value = key}};
            }

            IReadOnlyList<SelectListItem> GetDepictedEntitiesList()
            {
                var result = new List<SelectListItem>();

                foreach (var entity in depictedEntities)
                {
                    var title = entity.PageId is Guid id && pageLookup.TryGetValue(id, out var page)
                        ? page.Title
                        : entity.ObjectTitle;

                    result.Add(new SelectListItem
                    {
                        Selected = true,
                        Text = title,
                        Value = entity.PageId?.ToString() ?? entity.ObjectTitle
                    });
                }

                return result;
            }
        }

        #endregion
    }
}
