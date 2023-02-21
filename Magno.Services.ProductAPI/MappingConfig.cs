﻿using AutoMapper;
using Magno.Services.ProductAPI.Models;
using Magno.Services.ProductAPI.Models.Dto;

namespace Magno.Services.ProductAPI
{
    public class MappingConfig
    {
        public static MapperConfiguration RegisterMaps()
        {
            var mappingConfig = new MapperConfiguration(config =>
            {
                config.CreateMap<ProductDto, Product>();
                config.CreateMap<Product, ProductDto>();
            });

            return mappingConfig;
        }
    }
}
