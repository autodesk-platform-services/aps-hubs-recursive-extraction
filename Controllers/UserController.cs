/////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved
// Written by APS Partner Development
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted,
// provided that the above copyright notice appears in all copies and
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS.
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC.
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
/////////////////////////////////////////////////////////////////////

using Autodesk.Authentication.Model;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;


public class UserController : ControllerBase
{
  public readonly APSService _apsService;

  public UserController(APSService apsService)
  {
    _apsService = apsService;
  }
  [HttpGet]
  [Route("api/aps/user/profile")]
  public async Task<dynamic> GetUserProfileAsync()
  {
    Tokens tokens = OAuthController.PrepareTokens(Request, Response, _apsService).GetAwaiter().GetResult();
    if (tokens == null)
    {
      return null;
    }
    UserInfo userInfo = await _apsService.GetUserProfile(tokens);
    return new { name = userInfo.Name, picture = userInfo.Picture };

  }
}