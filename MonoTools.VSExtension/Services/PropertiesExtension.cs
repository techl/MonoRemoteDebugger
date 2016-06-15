using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;

namespace MonoTools.VSExtension {
	public static class PropertiesExtension {

		public static string Get(this Properties props, string name) {
			try {
				return props.Item("name")?.Value.ToString();
			} catch { }
			return null; 
		}
	}
}
