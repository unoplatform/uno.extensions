using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace ApplicationTemplate
{
	public class DataValidationState
	{
		public DataValidationState(DataValidationStateType stateType, ImmutableList<object> errors = null)
		{
			StateType = stateType;
			Errors = errors;
		}

		/// <summary>
		/// Gets the type of this validation state.
		/// </summary>
		public DataValidationStateType StateType { get; }

		/// <summary>
		/// Gets and error message that should be displayed by the view.
		/// </summary>
		public ImmutableList<object> Errors { get; }
	}
}
