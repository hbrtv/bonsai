﻿using System.Threading.Tasks;
using AutoMapper;
using Bonsai.Areas.Admin.ViewModels.DynamicConfig;
using Bonsai.Code.Services.Config;
using Bonsai.Data;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace Bonsai.Areas.Admin.Logic
{
    /// <summary>
    /// Service for managing the global app configuration.
    /// </summary>
    public class DynamicConfigManagerService
    {
        public DynamicConfigManagerService(AppDbContext db, IMapper mapper)
        {
            _db = db;
            _mapper = mapper;
        }

        private readonly AppDbContext _db;
        private readonly IMapper _mapper;

        /// <summary>
        /// Gets the update form default values.
        /// </summary>
        public async Task<UpdateDynamicConfigVM> RequestUpdateAsync()
        {
            var wrapper = await _db.DynamicConfig.FirstAsync();
            var config = JsonConvert.DeserializeObject<DynamicConfig>(wrapper.Value);
            return _mapper.Map<UpdateDynamicConfigVM>(config);
        }

        /// <summary>
        /// Updates the current configuration.
        /// </summary>
        public async Task UpdateAsync(UpdateDynamicConfigVM request)
        {
            var wrapper = await _db.DynamicConfig.FirstAsync();
            var config = _mapper.Map<DynamicConfig>(request);
            wrapper.Value = JsonConvert.SerializeObject(config);
        }
    }
}
