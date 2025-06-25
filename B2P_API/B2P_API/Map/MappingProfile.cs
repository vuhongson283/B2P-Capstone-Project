using AutoMapper;
using B2P_API.DTOs.Account;
using B2P_API.Models;
using Microsoft.AspNetCore.Identity.Data;
using Org.BouncyCastle.Crypto.Generators;

namespace B2P_API.Map
{
	public class MappingProfile : Profile
	{
		public MappingProfile()
		{
			CreateMap<User, GetListAccountResponse>()
			.ForMember(dest => dest.RoleName, opt => opt.MapFrom(src => src.Role.RoleName))
			.ForMember(dest => dest.StatusName, opt => opt.MapFrom(src => src.Status.StatusName));
		}
	}
}
