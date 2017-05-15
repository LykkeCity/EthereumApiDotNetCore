//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Threading.Tasks;
//using Services;
//using Common.Log;

//namespace EthereumJobs.Actions
//{
//    public class ProcessManualEvents
//    {
//		private readonly ILog _logger;
//	    private readonly IManualEventsService _manualEventService;

//		public ProcessManualEvents(ILog logger, IManualEventsService manualEventService)
//		{
//			_logger = logger;
//			_manualEventService = manualEventService;
//		}

//		public async Task Start()
//		{

//			try
//			{
//				while (await _manualEventService.ProcessManualEvent())
//				{
//				}
//			}
//			catch (Exception ex)
//			{
//				await _logger.WriteErrorAsync("EthereumJob", "ProcessManualEvents", "", ex);
//			}
//		}
//	}
//}
