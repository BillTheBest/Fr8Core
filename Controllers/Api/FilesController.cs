﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using HubWeb.Infrastructure;
using Microsoft.AspNet.Identity;
using StructureMap;
using Data.Entities;
using Data.Infrastructure.StructureMap;
using Data.Interfaces;
using Hub.Interfaces;
using System.Web.Http.Description;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using AutoMapper;


namespace HubWeb.Controllers
{
    //[Fr8ApiAuthorize]
    public class FilesController : ApiController
    {
        private readonly IFile _fileService;
        private readonly ISecurityServices _security;
        private readonly ITag _tagService;

        public FilesController() : this(ObjectFactory.GetInstance<IFile>()) { }

        public FilesController(IFile fileService)
        {
            _fileService = fileService;
            _security = ObjectFactory.GetInstance<ISecurityServices>();
            _tagService = ObjectFactory.GetInstance<ITag>();
        }

        [HttpPost]
        [ActionName("files")]
        [fr8HubWebHMACAuthorize]
        public async Task<IHttpActionResult> Post()
        {
            FileDO fileDO = null;

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                //TODO Create a standard way to get current user id on HMAC authentication
                var currentUserId = ((TerminalPrinciple) HttpContext.Current.User).GetOnBehalfUserId();


                await Request.Content.ReadAsMultipartAsync<MultipartMemoryStreamProvider>(new MultipartMemoryStreamProvider()).ContinueWith((tsk) =>
                {
                    MultipartMemoryStreamProvider prvdr = tsk.Result;

                    foreach (HttpContent ctnt in prvdr.Contents)
                    {
                        Stream stream = ctnt.ReadAsStreamAsync().Result;
                        var fileName = ctnt.Headers.ContentDisposition.FileName.Replace("\"", string.Empty);
                        fileDO = new FileDO
                        {
                            DockyardAccountID = currentUserId
                        };

                        _fileService.Store(uow, fileDO, stream, fileName);

                    }
                });

                return Ok(fileDO);
            }
        }

        [HttpGet]
        //[Route("files/details/{id:int}")]
        [ActionName("details")]
        [ResponseType(typeof(FileDTO))]
        public IHttpActionResult Details(int id)
        {
            FileDTO fileDto = null;

            if (_security.IsCurrentUserHasRole(Roles.Admin))
            {
                fileDto = Mapper.Map<FileDTO>(_fileService.GetFileByAdmin(id));
            }
            else
            {
                string userId;

                using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    userId = _security.GetCurrentAccount(uow).Id;
                }

                fileDto = Mapper.Map<FileDTO>(_fileService.GetFile(id, userId));
            }

            return Ok(fileDto);
        }

        public IHttpActionResult Get()
        {
            IList<FileDTO> fileList;

            if (_security.IsCurrentUserHasRole(Roles.Admin))
            {
                fileList = Mapper.Map<IList<FileDTO>>(_fileService.AllFilesList());
            }
            else
            {
                string userId;

                using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
                {
                    userId = _security.GetCurrentAccount(uow).Id;
                }

                fileList = Mapper.Map<IList<FileDTO>>(_fileService.FilesList(userId));
            }

            // fills Tags property for each fileDTO to display in the Tags column
            // example: { "key1" : "value1" }, {"key2", "value2} ...
            foreach (var file in fileList)
            {
                var result = String.Empty;
                var tags = _tagService.GetList(file.Id);
                bool isFirstItem = true;
                foreach (var tag in tags)
                {
                    if (isFirstItem)
                    {
                        isFirstItem = false;
                    }
                    else
                    {
                        result += ", ";
                    }
                    result += "{\"" + tag.Key + "\" : \"" + tag.Value + "\"}";
                }
                file.Tags = result;
            }

            return Ok(fileList);
        }

        public void Delete(int id)
        {
            _fileService.Delete(id);
        }
    }
}
