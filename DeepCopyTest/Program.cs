using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DeepCopyTest
{ 
    class Program
    {
        static void Main(string[] args)
        {

            MyNamespace.BaseClass<MyNamespace.Test> baseClass = new MyNamespace.BaseClass<MyNamespace.Test>();


            baseClass.payload = new MyNamespace.Test();
            baseClass.payload.listValueType = new List<string>();// .Add("str");
            baseClass.payload.listValueType.Add("D");
            baseClass.payload.listValueType.Add("jek");
            baseClass.payload.listValueType.Add("vad");
            baseClass.Error = new MyNamespace.ErrorClass { Description = "Erorr Detected", ID = 100 };

            BaseClass<Test> clonedObjTest = (BaseClass<Test>)DeepCopyObjectCopy(baseClass, typeof(BaseClass<Test>));



            MyNamespace.Test t = new MyNamespace.Test();

            Test clonedObj = (Test)DeepCopyObjectCopy(t, typeof(Test));

        }


		public static object DeepCopyObjectCopy<T>(T source, Type destinationType, bool isDynamicType = false)
		{

			object destinationObject = Activator.CreateInstance(destinationType);
			string nameSpace = source.GetType().Namespace;
			PropertyInfo[] propertyInfosSource, propertyInfosDestination;
			propertyInfosSource = source.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
			propertyInfosDestination = destinationObject.GetType().GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.FlattenHierarchy);


			foreach (PropertyInfo p in propertyInfosSource)
			{


				PropertyInfo pReference = propertyInfosDestination.FirstOrDefault(e => e.Name == p.Name);
				object valObj = p.GetValue(source, null);

				if (valObj != null && pReference != null)//// 
				{


					//When regular type or object from desired namespace
					if (!valObj.GetType().IsGenericType)
					{

						if (valObj.GetType().Namespace != nameSpace)
							pReference.SetValue(destinationObject, valObj, null);
						else
						{
							if ((valObj.GetType().BaseType == typeof(Enum)))
							{
								pReference.SetValue(destinationObject, valObj, null);
							}
							else
							{
								object obj = DeepCopyObjectCopy(valObj, Type.GetType(pReference.PropertyType.FullName));
								pReference.SetValue(destinationObject, obj, null);
							}
						}
					}

					//Dictionary
					#region DICTIONARY
					if (p.PropertyType.Name == typeof(Dictionary<,>).Name)
					{
						var DictionaryType = Type.GetType(pReference.PropertyType.FullName);


						Type[] arguments = pReference.PropertyType.GetGenericArguments(); //types of dicationary arguments: key & value

						Type keyType = arguments[0];    //type of dictionary key
						Type valueType = arguments[1];  //type of dictionary value


						Type dictToCreate = typeof(Dictionary<,>).MakeGenericType(arguments);
						var Result = (IDictionary)Activator.CreateInstance(dictToCreate);


						foreach (DictionaryEntry item in (IDictionary)valObj)
						{
							object keyObj, valueObj;

							if (keyType == typeof(string))
								keyObj = item.Key;
							else
							{
								if (keyType.IsValueType)
									keyObj = DeepCopyObjectCopy(item.Key, keyType);
								else
									keyObj = item.Key;
							}

							if (valueType == typeof(string))
								valueObj = item.Value;
							else
							{

								if (valueType.IsValueType)
									valueObj = DeepCopyObjectCopy(item.Value, valueType);
								else
									valueObj = item.Value;
							}

							var add = Result.GetType().GetMethod("Add", new[] { keyType, valueType });
							Result.GetType().GetMethod("Add").Invoke(Result, new object[] { keyObj, valueObj });

						}

						pReference.SetValue(destinationObject, Result, null);

					}
					#endregion
					//When Object is generic list with type of desired object to copy 
					if (valObj.GetType().IsGenericType && p.PropertyType.Name != typeof(Dictionary<,>).Name)
					{

						var listType = Type.GetType(pReference.PropertyType.FullName);

						Type itemType;
						#region Array Generic Type Copy
						if (listType.IsArray)
						{
							itemType = listType.GetElementType();


							object d = Array.CreateInstance(itemType, ((IList)valObj).Count);

							int ind = ((IList)valObj).Count;


							for (int i = 0; i < ind; i++)
							{

								if (((IList)valObj)[i].GetType() == typeof(string))
								{
									object f = ((IList)valObj)[i];
									((T[])d)[i] = (T)f;

								}

							}
							pReference.SetValue(destinationObject, d, null);


						}
						#endregion 
						else
						{

							itemType = listType.GetGenericArguments()[0];


							var IListRef = typeof(List<>);
							Type[] IListParam = { itemType };
							//itemType
							object Result = Activator.CreateInstance(IListRef.MakeGenericType(IListParam));
							foreach (var item in (IList)valObj)
							{

								if (item.GetType() == typeof(string) || item.GetType().IsValueType) //String or ValueType
								{
									Result.GetType().GetMethod("Add").Invoke(Result, new[] { item });
								}
								else
								{
									//Get Run Time Type of Item!!!

									if (itemType.IsGenericType)
									{
										var InnerlistType = Type.GetType(itemType.FullName);
										Type itemInnerType = InnerlistType.GetGenericArguments()[0];
										var IInnerListRef = typeof(List<>);
										Type[] IInnerListParam = { itemInnerType };

										object InnerResult = Activator.CreateInstance(IInnerListRef.MakeGenericType(IInnerListParam));

										object obj = CopyList(item, itemType);

										//object obj = DeepCopyObjectCopy(item, itemType);
										Result.GetType().GetMethod("Add").Invoke(Result, new[] { obj });
									}
									else
									{
										var o = Activator.CreateInstance(itemType.Assembly.FullName, itemType.Namespace + "." + item.GetType().Name);

										Type dynamicType = o.Unwrap().GetType();
										object obj = DeepCopyObjectCopy(item, dynamicType);
										Result.GetType().GetMethod("Add").Invoke(Result, new[] { obj });
									}

								

								}
							}

							pReference.SetValue(destinationObject, Result, null);

						}
					}
				}

			}


			return destinationObject;
		}



		public static object CopyList<T>(T Source, Type destinationType)
		{
		   object destinationObject = Activator.CreateInstance(destinationType);

		   if (destinationType.IsGenericType) {
			  var  itemType = destinationType.GetGenericArguments()[0];
			   var IListRef = typeof(List<>);
			   Type[] IListParam = { itemType };
			   //itemType
			   object Result = Activator.CreateInstance(IListRef.MakeGenericType(IListParam));
			   foreach (var item in (IList)Source)
			   {

				   if (item.GetType() == typeof(string) || item.GetType().IsValueType) //String or ValueType
				   {
					   Result.GetType().GetMethod("Add").Invoke(Result, new[] { item });
				   }
				   else
				   {

					   // CREATE NEW UPDA FOR LIST IN LIST NEw UPdate For LISt IN LIST 
					   //Get Run Time Type of Item!!!

					   if (itemType.IsGenericType)
					   {
						   object obj = DeepCopyObjectCopy(item, itemType);
						   Result.GetType().GetMethod("Add").Invoke(Result, new[] { obj });
					   }
					   else
					   {
						   var o = Activator.CreateInstance(itemType.Assembly.FullName, itemType.Namespace + "." + item.GetType().Name);
						   Type dynamicType = o.Unwrap().GetType();
						   object obj = DeepCopyObjectCopy(item, dynamicType);
						   Result.GetType().GetMethod("Add").Invoke(Result, new[] { obj });
					   }

           

				   }
			   }
			   destinationObject = Result;
		   }
		   return destinationObject;

		}

    }



    public class BaseClass<T>
    {

        public ErrorClass Error { get; set; }
        public T payload { get; set; }

        public anotherMember EnotherMember { get; set; }

    }


    public class ErrorClass
    {


        public int ID { get; set; }
        public string Description { get; set; }

    }
    public class Test
    {

        public string _strValue;
        public bool _boolValue;
        public int _intValue;
        public List<List<anotherMember>> _listValue = new List<List<anotherMember>>();

        anotherMember _anotheMemberValue = new anotherMember { memberValue = 10 };
        public string strValue { get { return _strValue; } set { _strValue = value; } }
        public bool boolValue { get { return _boolValue; } set { _boolValue = value; } }
        public int intValue { get { return _intValue; } set { _intValue = value; } }


        public List<string> listValueType { get; set; }
        public List<List<anotherMember>> listValue { get { return _listValue; } set { _listValue = value; } }

        public anotherMember anotheMemberValue { get { return _anotheMemberValue; } set { _anotheMemberValue = value; } }



    }

    public class anotherMember
    {

        int _member = 10;
        public int memberValue { get { return _member; } set { _member = value; } }

    }

}

namespace MyNamespace
{


    public class BaseClass<T>
    {

        public ErrorClass Error { get; set; }
        public T payload { get; set; }

        public anotherMember EnotherMember { get; set; }

    }


    public class ErrorClass
    {
        public int ID { get; set; }
        public string Description { get; set; }

    }

    public class Test
    {

        public string _strValue = "Vadim";
        public bool _boolValue = true;
        public int _intValue = 10;
        public List<string> _listValueType = new List<string> { "", "", "" };

        public List<List<anotherMember>> _listValue = new List<List<anotherMember>>() { 
			new List<anotherMember> {new anotherMember{memberValue = 10},new anotherMember{memberValue = 1000},new anotherMember{memberValue = 30}}
                                                                         ,new List<anotherMember> {new anotherMember {memberValue = 20}}
                                                                         ,new List<anotherMember> {new anotherMember {memberValue = 30}}};
        anotherMember _anotheMemberValue = new anotherMember { memberValue = 10 };
        public string strValue { get { return _strValue; } set { _strValue = value; } }
        public bool boolValue { get { return _boolValue; } set { _boolValue = value; } }
        public int intValue { get { return _intValue; } set { _intValue = value; } }


        public List<List<anotherMember>> listValue { get { return _listValue; } set { _listValue = value; } }

        public List<string> listValueType { get { return _listValueType; } set { _listValueType = value; } }



        public anotherMember anotheMemberValue { get { return _anotheMemberValue; } set { _anotheMemberValue = value; } }
        public Test()
        {
            //   listValue = new List<anotherMember>();
        }
    }

    public class anotherMember
    {

        int _member = 10;
        public int memberValue { get { return _member; } set { _member = value; } }

    }

}
