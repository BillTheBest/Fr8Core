﻿using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data.States.Templates
{
	public class _ProcessNodeStatusTemplate: IStateTemplate< ProcessNodeState >
	{
		[ Key ]
		[ DatabaseGenerated( DatabaseGeneratedOption.None ) ]
		public int Id{ get; set; }

		public string Name{ get; set; }

		public override string ToString()
		{
			return this.Name;
		}
	}
}