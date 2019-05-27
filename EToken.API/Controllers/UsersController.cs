﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using EToken.API.Helpers;
using EToken.Core.Services;
using EToken.Infrustructure.Resource;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using EToken.API.Extensions;
using EToken.Core.Model;
using Microsoft.AspNetCore.Cors;

namespace EToken.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private IUserService _userService;
        private IMapper _mapper;
        private readonly AppSettings _appSettings;

        public UsersController(
            IUserService userService
            ,IMapper mapper
            ,IOptions<AppSettings> appSetting)
        {
            _userService = userService;
            _mapper = mapper;
            _appSettings = appSetting.Value;
        }
        /// <summary>
        /// Authenticate a user Resource
        /// </summary>
        /// <param name="userResource">Inject a UserResource</param>
        /// <returns>Returns username,firstname,lastname and token string</returns>
        [AllowAnonymous]
        [HttpPost("authenticate")]
        public async Task<IActionResult> Authenticate([FromBody]UserResource userResource)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());
            var user = await _userService.Authenticate(userResource.Username, userResource.Password);

            if (user == null)
                return BadRequest(new { message = "Username or password is incorrect" });

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_appSettings.Secret);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, user.Id.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = tokenHandler.WriteToken(token);

            // return basic user info (without password) and token to store client side
            return Ok(new
            {
                Id = user.Id,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Token = tokenString
            });
        }
        /// <summary>
        /// Create a User Resource
        /// </summary>
        /// <param name="userResource">Inject a user Resource from client</param>
        /// <returns>Inject a User Resource</returns>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody]UserResource userResource)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());
            // map dto to entity
            var user = _mapper.Map<User>(userResource);

            try
            {
                // save 
                var result = await _userService.CreateAsync(user, userResource.Password);
                if (!result.Success)
                    return BadRequest(result.Message);
                var userData = _mapper.Map<User, UserResource>(result.user);
                return Ok(userData);
            }
            catch (AppExceptions ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }
        /// <summary>
        /// Get all the resources 
        /// </summary>
        /// <returns>Returns the resource in User Resource Format</returns>

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userService.GetAllAsync();
            var userResources = _mapper.Map<IList<UserResource>>(users);
            return Ok(userResources);
        }
        /// <summary>
        /// Get a single user resource by userResource ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns>User Resource</returns>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var user = await _userService.GetById(id);
            var userResource = _mapper.Map<UserResource>(user);
            return Ok(userResource);
        }
        /// <summary>
        /// Updates a User Resource
        /// </summary>
        /// <param name="id">Integer</param>
        /// <param name="userResource">Inject User Resource</param>
        /// <returns>Returns the updated resource</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody]UserResource userResource)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState.GetErrorMessages());
            // map dto to entity and set id
            var user = _mapper.Map<User>(userResource);
            user.Id = id;

            try
            {
                // save 
                var result = await _userService.UpdateAsync(user, userResource.Password);
                if (!result.Success)
                    return BadRequest(result.Message);
                var userData = _mapper.Map<User, UserResource>(result.user);
                return Ok(userData);
            }
            catch (AppExceptions ex)
            {
                // return error message if there was an exception
                return BadRequest(new { message = ex.Message });
            }
        }
        /// <summary>
        /// Deletes a User Resource
        /// </summary>
        /// <param name="id">Id</param>
        /// <returns>User Resource</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _userService.DeleteAsync(id);
            if (!result.Success)
                return BadRequest(result.Message);
            var userResource = _mapper.Map<User, UserResource>(result.user);
            return Ok(userResource);
        }
    }
}