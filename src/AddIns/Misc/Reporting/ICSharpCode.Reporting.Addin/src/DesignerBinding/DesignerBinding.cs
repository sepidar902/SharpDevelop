﻿/*
 * Created by SharpDevelop.
 * User: Peter Forstmeier
 * Date: 11.02.2014
 * Time: 20:19
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.IO;
using System.Xml;
using ICSharpCode.Core;
using ICSharpCode.SharpDevelop.Workbench;
using ICSharpCode.Reporting.Addin.Commands;
using ICSharpCode.Reporting.Addin.Factory;
using ICSharpCode.Reporting.Addin.ReportWizard;

namespace ICSharpCode.Reporting.Addin.DesignerBinding {
	
	public class ReportDesignerBinding:IDisplayBinding {
		
		public bool IsPreferredBindingForFile(FileName fileName){
			return true;
		}
		
		
		public bool CanCreateContentForFile(FileName fileName){
			return Path.GetExtension(fileName).Equals(".srd", StringComparison.OrdinalIgnoreCase);
		}
		
		
		public double AutoDetectFileContent(FileName fileName, System.IO.Stream fileContent, string detectedMimeType){
			throw new NotImplementedException();
		}
		
		
		public IViewContent CreateContentForFile(OpenedFile file){
			if (file.IsDirty) {
				var cmd = new ReportWizardCommand();
				cmd.Run();
				if (!cmd.Canceled) {
					var reportModel = cmd.ReportModel;
					var xml = CreateFormSheetFromModel.ToXml(reportModel);
					var doc = new XmlDocument();
					doc.LoadXml(xml.ToString());
					var ar = XmlToArray(doc);
					file.SetData(ar);
				} else {
					LoggingService.Info("ReportWizard canceled");
					return null;
				}
			}

			var viewCmd = new CreateDesignerCommand(file);
			viewCmd.Run();
			LoggingService.Info("DesignerBinding -> Designer started");
			return viewCmd.DesignerView;
		}

		
		static byte[] XmlToArray(XmlDocument doc){
			using (var stream = new MemoryStream()) {
				doc.Save(stream);
				return stream.ToArray();
			}
		}
	}
}