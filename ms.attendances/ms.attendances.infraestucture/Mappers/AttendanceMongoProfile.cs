﻿using AutoMapper;
using ms.attendances.domain.Entities;
using ms.attendances.infraestucture.MongoEntities;

namespace ms.attendances.infraestucture.Mappers
{
    public class AttendanceMongoProfile: Profile
    {
        public AttendanceMongoProfile() =>
            CreateMap<AttendanceMongo, AttendanceRecord>().ReverseMap();
    }
}
