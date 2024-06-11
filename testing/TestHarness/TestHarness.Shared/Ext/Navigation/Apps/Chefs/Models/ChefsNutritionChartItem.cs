
namespace TestHarness.Ext.Navigation.Apps.Chefs.Models;

public partial record ChefsNutritionChartItem
{
	public ChefsNutritionChartItem(int chartTrackVal = 1000)
	{
		Value = chartTrackVal;
	}

	//public ChefsNutritionChartItem(string? name, double? value, double? maxValueRef, SolidColorPaint? columnColor = default)
	//{
	//	Name = name;
	//	ColumnColor = columnColor;
	//	ChartProgressVal = value;

	//	var _val = value ?? 0;
	//	var _maxValueRef = maxValueRef ?? 0;
	//	var _tempValue = (_val / _maxValueRef) * 100;

	//	Value = _tempValue * 10;
	//	MaxValueRef = _maxValueRef;
	//}

	public string? Name { get; }

	public double? ChartProgressVal { get; }

	public double Value { get; }

	public double MaxValueRef { get; }

	//public SolidColorPaint? ColumnColor { get; }
}
