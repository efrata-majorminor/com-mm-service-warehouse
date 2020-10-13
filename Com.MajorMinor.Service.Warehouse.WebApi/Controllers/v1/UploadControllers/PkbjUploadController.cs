using Microsoft.AspNetCore.Mvc;
using Com.MM.Service.Warehouse.Lib;
using Com.MM.Service.Warehouse.Lib.Services;
using Com.MM.Service.Warehouse.WebApi.Helpers;
using Com.MM.Service.Warehouse.Lib.Models;
using Com.MM.Service.Warehouse.Lib.ViewModels;
using Com.MM.Service.Warehouse.Lib.Facades;
using Com.MM.Service.Warehouse.Lib.Models.SPKDocsModel;
using Com.MM.Service.Warehouse.Lib.ViewModels.SpkDocsViewModel;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using AutoMapper;
using System.Linq;
using CsvHelper;
using Microsoft.AspNetCore.Authorization;
using static Com.MM.Service.Warehouse.Lib.Facades.PkpbjFacade;
using Com.MajorMinor.Service.Warehouse.Lib.ViewModels.SpkDocsViewModel;
using Com.MM.Service.Warehouse.Lib.Interfaces.PkbjInterfaces;

namespace Com.MM.Service.Core.WebApi.Controllers.v1.UploadControllers
{
    [Produces("application/json")]
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/warehouse/upload-pkbj")]
    [Authorize]
    public class PkpbjUploadController : Controller
    //: BasicUploadController<PkpbjFacade, SPKDocs, SPKDocsViewModel, PkpbjFacade.PkbjMap, WarehouseDbContext>
    {
        private string ApiVersion = "1.0.0";
        private readonly IMapper mapper;
        private readonly IPkpbjFacade facade;
        private readonly IdentityService identityService;
        private readonly string ContentType = "application/vnd.openxmlformats";
        private readonly string FileName = string.Concat("Error Log - ", typeof(SPKDocs).Name, " ", DateTime.Now.ToString("dd MMM yyyy"), ".csv");
        public PkpbjUploadController(IMapper mapper, IPkpbjFacade facade, IdentityService identityService) //: base(facade, ApiVersion)
        {
            this.mapper = mapper;
            this.facade = facade;
            this.identityService = identityService;
        }

        //private Action<COAModel> Transfrom => (coaModel) =>
        //{
        //    var codeArray = coaModel.Code.Split('.');
        //    coaModel.Code1 = codeArray[0];
        //    coaModel.Code2 = codeArray[1];
        //    coaModel.Code3 = codeArray[2];
        //    coaModel.Code4 = codeArray[3];
        //    coaModel.Header = coaModel.Code.Substring(0, 1);
        //    coaModel.Subheader = coaModel.Code.Substring(0, 2);

        //};
        [HttpPost("upload")]
        public async Task<IActionResult> PostCSVFileAsync(double source, string sourcec, string sourcen, double destination, string destinationc, string destinationn, DateTimeOffset date)
        // public async Task<IActionResult> PostCSVFileAsync(double source, double destination,  DateTime date)
        {
            try
            {
                identityService.Username = User.Claims.Single(p => p.Type.Equals("username")).Value;
                identityService.Token = Request.Headers["Authorization"].FirstOrDefault().Replace("Bearer ", "");
                identityService.TimezoneOffset = Convert.ToInt32(Request.Headers["x-timezone-offset"]);
                if (Request.Form.Files.Count > 0)
                {
                    //VerifyUser();
                    var UploadedFile = Request.Form.Files[0];
                    StreamReader Reader = new StreamReader(UploadedFile.OpenReadStream());
                    List<string> FileHeader = new List<string>(Reader.ReadLine().Split(","));
                    var ValidHeader = facade.CsvHeader.SequenceEqual(FileHeader, StringComparer.OrdinalIgnoreCase);

                    if (ValidHeader)
                    {
                        Reader.DiscardBufferedData();
                        Reader.BaseStream.Seek(0, SeekOrigin.Begin);
                        Reader.BaseStream.Position = 0;
                        CsvReader Csv = new CsvReader(Reader);
                        Csv.Configuration.IgnoreQuotes = false;
                        Csv.Configuration.Delimiter = ",";
                        Csv.Configuration.RegisterClassMap<PkbjMap>();
                        Csv.Configuration.HeaderValidated = null;

                        List<SPKDocsCsvViewModel> Data = Csv.GetRecords<SPKDocsCsvViewModel>().ToList();

                        SPKDocsViewModel Data1 = await facade.MapToViewModel(Data, source, sourcec, sourcen, destination, destinationc, destinationn, date);

                        Tuple<bool, List<object>> Validated = facade.UploadValidate(ref Data, Request.Form.ToList());

                        Reader.Close();

                        if (Validated.Item1) /* If Data Valid */
                        {
                            SPKDocs data = mapper.Map<SPKDocs>(Data1);
                            //foreach (var item in data)
                            //{
                            //    Transfrom(item);
                            //}
                            await facade.UploadData(data, identityService.Username);


                            Dictionary<string, object> Result =
                                new ResultFormatter(ApiVersion, General.CREATED_STATUS_CODE, General.OK_MESSAGE)
                                .Ok();
                            return Created(HttpContext.Request.Path, Result);

                        }
                        else
                        {
                            using (MemoryStream memoryStream = new MemoryStream())
                            {
                                using (StreamWriter streamWriter = new StreamWriter(memoryStream))
                                using (CsvWriter csvWriter = new CsvWriter(streamWriter))
                                {
                                    csvWriter.WriteRecords(Validated.Item2);
                                }

                                return File(memoryStream.ToArray(), ContentType, FileName);
                            }
                        }
                    }
                    else
                    {
                        Dictionary<string, object> Result =
                           new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, General.CSV_ERROR_MESSAGE)
                           .Fail();

                        return NotFound(Result);
                    }
                }
                else
                {
                    Dictionary<string, object> Result =
                        new ResultFormatter(ApiVersion, General.BAD_REQUEST_STATUS_CODE, General.NO_FILE_ERROR_MESSAGE)
                            .Fail();
                    return BadRequest(Result);
                }
            }
            catch (Exception e)
            {
                Dictionary<string, object> Result =
                   new ResultFormatter(ApiVersion, General.INTERNAL_ERROR_STATUS_CODE, e.Message)
                   .Fail();

                return StatusCode(General.INTERNAL_ERROR_STATUS_CODE, Result);
            }
        }

    }


}

