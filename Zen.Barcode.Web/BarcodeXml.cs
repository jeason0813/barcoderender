//-----------------------------------------------------------------------
// <copyright file="BarcodeXml.cs" company="Zen Design Corp">
//     Copyright � Zen Design Corp 2008 - 2011. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Zen.Barcode.Web
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Drawing.Design;
    using System.IO;
    using System.Security.Permissions;
    using System.Text;
    using System.Web;
    using System.Web.Caching;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using System.Xml;
    using System.Xml.XPath;
    using System.Xml.Xsl;

    using Zen.Barcode.Web.Xsl;

    /// <summary>
	/// <b>BarcodeXml</b> web-control replaces <see cref="T:Xml"/> by 
	/// automatically registering our custom extension object that is used to
	/// translate design IDs and barcode text into image URIs suitable for use
	/// in the transformed HTML - uber cool!
	/// </summary>
	[
	ToolboxData ("<{0}:BarcodeXml runat=server></{0}:BarcodeXml>"),
	Designer ("Zen.Barcode.Web.Design.BarcodeXmlDesigner, Zen.Barcode.Design, Culture=Neutral, Version=2.1.0.0, PublicKeyToken=b5ae55aa76d2d9de"),
	DefaultProperty ("DocumentSource"), 
	PersistChildren (false, true), 
	ControlBuilder (typeof (XmlBuilder))//, 
	//AspNetHostingPermission (SecurityAction.LinkDemand, Level = AspNetHostingPermissionLevel.Minimal),
	//AspNetHostingPermission (SecurityAction.InheritanceDemand, Level = AspNetHostingPermissionLevel.Minimal)
	]
	public class BarcodeXml : Control
	{
		#region Private Fields
		private XPathDocument _xpathDocument;
		private XPathNavigator _xpathNavigator;

		private string _documentContent;
		private string _documentSource;
		private string _transformSource;
		private XslCompiledTransform _transform;
		private XsltArgumentList _transformArgumentList;

		private static XslCompiledTransform _identityTransform;
		private const string identityXslStr = "<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'><xsl:template match=\"/\"> <xsl:copy-of select=\".\"/> </xsl:template> </xsl:stylesheet>";
		#endregion

		#region Public Constructors
		/// <summary>
		/// Static constructor for <see cref="T:BarcodeXml" />
		/// </summary>
		static BarcodeXml()
		{
			XmlTextReader reader = new XmlTextReader (new StringReader(identityXslStr));
			_identityTransform = new XslCompiledTransform ();
			_identityTransform.Load(reader, null, null);
		}

		/// <summary>
		/// Initialises an instance of <see cref="T:BarcodeXml" />.
		/// </summary>
		public BarcodeXml ()
		{
		}
		#endregion

		#region Public Properties
		/// <summary>
		/// Gets the server control identifier generated by ASP.NET.
		/// </summary>
		/// <value></value>
		/// <returns>The server control identifier generated by ASP.NET.</returns>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public override string ClientID
		{
			get
			{
				return base.ClientID;
			}
		}

		/// <summary>
		/// Gets a <see cref="T:System.Web.UI.ControlCollection"></see> object that represents the child controls for a specified server control in the UI hierarchy.
		/// </summary>
		/// <value></value>
		/// <returns>The collection of child controls for the specified server control.</returns>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public override ControlCollection Controls
		{
			get
			{
				return base.Controls;
			}
		}

		/// <summary>
		/// Gets/sets a string that contains the XML document to display in
		/// the <see cref="T:BarcodeXml" /> control.
		/// </summary>
		/// <returns>
		/// A string that contains the XML document to display in the 
		/// <see cref="T:BarcodeXml" /> control.
		/// </returns>
		[
		Description ("Gets/sets a string that contains the XML document to display in the control."),
		DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden),
		Browsable (false)
		]
		public string DocumentContent
		{
			get
			{
				if (_documentContent == null)
				{
					return string.Empty;
				}
				return _documentContent;
			}
			set
			{
				_xpathDocument = null;
				_xpathNavigator = null;
				_documentContent = value;
				if (base.DesignMode)
				{
					ViewState["OriginalContent"] = null;
				}
			}
		}

		/// <summary>
		/// Gets or sets the path to an XML document to display in the 
		/// <see cref="T:BarcodeXml" /> control.
		/// </summary>
		/// <returns>
		/// The path to an XML document to display in the <see cref="T:BarcodeXml" /> control.
		/// </returns>
		[
		Category ("Behavior"), 
		Editor ("System.Web.UI.Design.XmlUrlEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof (UITypeEditor)),
		Description ("Gets or sets the path to an XML document to display in the control."), 
		DefaultValue (""), 
		UrlProperty
		]
		public string DocumentSource
		{
			get
			{
				if (_documentSource != null)
				{
					return _documentSource;
				}
				return string.Empty;
			}
			set
			{
				_xpathDocument = null;
				_documentContent = null;
				_xpathNavigator = null;
				_documentSource = value;
			}
		}

		/// <summary>
		/// Overrides the <see cref="P:System.Web.UI.Control.EnableTheming" />
		/// property. This property is not supported by the <see cref="T:BarcodeXml" /> class.
		/// </summary>
		/// <returns>
		/// Always returns false. This property is not supported.
		/// </returns>
		/// <exception cref="T:System.NotSupportedException">
		/// An attempt is made to set the value of this property.
		/// </exception>
		[
		DefaultValue (false), 
		Browsable (false), 
		EditorBrowsable (EditorBrowsableState.Never)
		]
		public override bool EnableTheming
		{
			get
			{
				return false;
			}
			set
			{
				throw new NotSupportedException ("No Theming Support");
			}
		}

		/// <summary>
		/// Overrides the <see cref="P:System.Web.UI.Control.SkinID" />
		/// property. This property is not supported by the 
		/// <see cref="T:BarcodeXml" /> class.
		/// </summary>
		/// <returns>
		/// Always returns an empty string (""). This property is not supported.
		/// </returns>
		/// <exception cref="T:System.NotSupportedException">
		/// An attempt is made to set the value of this property.
		/// </exception>
		[
		Browsable (false), 
		EditorBrowsable (EditorBrowsableState.Never),
		DefaultValue ("")
		]
		public override string SkinID
		{
			get
			{
				return string.Empty;
			}
			set
			{
				throw new NotSupportedException ("No Theming Support");
			}
		}

		/// <summary>
		/// Gets or sets the <see cref="T:System.Xml.Xsl.XslTransform" /> 
		/// object that formats the XML document before it is written to the
		/// output stream.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.Xml.Xsl.XslTransform" /> that formats the
		/// XML document before it is written to the output stream.
		/// </returns>
		[
		DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden),
		Browsable (false), 
		Description ("Gets or sets the XslTransform object that formats the XML document before it is written to the output stream.")
		]
		public XslCompiledTransform Transform
		{
			get
			{
				return _transform;
			}
			set
			{
				TransformSource = null;
				_transform = value;
			}
		}

		/// <summary>
		/// Gets or sets a <see cref="T:System.Xml.Xsl.XsltArgumentList" />
		/// that contains a list of optional arguments passed to the style 
		/// sheet and used during the Extensible Stylesheet Language 
		/// Transformation (XSLT).
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Xml.Xsl.XsltArgumentList" /> that contains
		/// a list of optional arguments passed to the style sheet and used
		/// during the XSL Transformation.
		/// </returns>
		[
		DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden),
		Browsable (false), 
		Description ("Gets or sets the XSL transformation arguments.")
		]
		public XsltArgumentList TransformArgumentList
		{
			get
			{
				return _transformArgumentList;
			}
			set
			{
				_transformArgumentList = value;
			}
		}

		/// <summary>
		/// Gets or sets the path to an Extensible Stylesheet Language 
		/// Transformation (XSLT) style sheet that formats the XML document
		/// before it is written to the output stream.
		/// </summary>
		/// <returns>
		/// The path to an XSL Transformation style sheet that formats the
		/// XML document before it is written to the output stream.
		/// </returns>
		[
		Description ("Gets or sets the path to an Extensible Stylesheet Language Transformation (XSLT) style sheet"),
		Editor ("System.Web.UI.Design.XslUrlEditor, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", typeof (UITypeEditor)),
		DefaultValue (""),
		Category ("Behavior")
		]
		public string TransformSource
		{
			get
			{
				if (_transformSource != null)
				{
					return _transformSource;
				}
				return string.Empty;
			}
			set
			{
				_transform = null;
				_transformSource = value;
			}
		}

		/// <summary>
		/// Gets or sets a cursor model for navigating and editing the XML
		/// data associated with the <see cref="T:BarcodeXml" /> control.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Xml.XPath.XPathNavigator" /> object.
		/// </returns>
		[
		DesignerSerializationVisibility (DesignerSerializationVisibility.Hidden),
		Description ("Gets or sets a cursor model for navigating and editing the XML data."),
		Browsable (false)
		]
		public XPathNavigator XPathNavigator
		{
			get
			{
				return _xpathNavigator;
			}
			set
			{
				DocumentSource = null;
				_xpathDocument = null;
				_documentContent = null;
				_xpathNavigator = value;
			}
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Registers the specified extension object under the given namespace
		/// allowing methods on the object to be called by XSLT style-sheets.
		/// </summary>
		/// <param name="namespaceUri">
		/// A <see cref="T:String"/> containing the namespace.
		/// </param>
		/// <param name="extension">
		/// An extension object.
		/// </param>
		public void RegisterExtensionObject (string namespaceUri, object extension)
		{
			// Sanity checks
			if (extension == null)
			{
				throw new ArgumentNullException ("extension");
			}

			// Allocate as required
			if (_transformArgumentList == null)
			{
				_transformArgumentList = new XsltArgumentList ();
			}

			// Add extension object
			_transformArgumentList.AddExtensionObject (namespaceUri, extension);
		}
		#endregion

		#region Protected Methods
		/// <summary>
		/// Notifies the server control that an element, either XML or HTML,
		/// was parsed, and adds the element to the server control's
		/// <see cref="T:System.Web.UI.ControlCollection" /> object.
		/// </summary>
		/// <param name="obj">
		/// An <see cref="T:System.Object" /> that represents the 
		/// <see cref="T:System.Web.UI.LiteralControl" /> to add.
		/// </param>
		/// <exception cref="T:System.Web.HttpException">
		/// obj is not of type <see cref="T:System.Web.UI.LiteralControl"/>.
		/// </exception>
		protected override void AddParsedSubObject (object obj)
		{
			if (!(obj is LiteralControl))
			{
				throw new HttpException ("Cannot Have Children Of Type other than LiteralControl");
			}

			string literalText = ((LiteralControl) obj).Text;
			//int num1 = System.Web.Util.FirstNonWhiteSpaceIndex (text1);
			//DocumentContent = text1.Substring (num1);
			DocumentContent = literalText.TrimStart (
				new char[] { ' ', '\t' });
			if (base.DesignMode)
			{
				ViewState["OriginalContent"] = literalText;
			}
		}

		/// <summary>
		/// Creates a new <see cref="T:System.Web.UI.EmptyControlCollection" />
		/// object.
		/// </summary>
		/// <returns>
		/// Always returns an <see cref="T:System.Web.UI.EmptyControlCollection" />.
		/// </returns>
		protected override ControlCollection CreateControlCollection ()
		{
			return new EmptyControlCollection (this);
		}

		/// <summary>
		/// Searches the page naming container for the specified server control.
		/// </summary>
		/// <returns>
		/// The specified control, or null if the specified control does not
		/// exist.
		/// </returns>
		/// <param name="id">
		/// The identifier for the control to be found.
		/// </param>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public override Control FindControl (string id)
		{
			return base.FindControl (id);
		}

		/// <summary>
		/// Overrides the <see cref="M:System.Web.UI.Control.Focus" /> method. 
		/// This method is not supported by the <see cref="T:BarcodeXml" /> class.
		/// </summary>
		/// <exception cref="T:System.NotSupportedException">An attempt is made to invoke this method.</exception>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public override void Focus ()
		{
			throw new NotSupportedException ("No Focus Support");
		}

		/// <summary>
		/// Gets design-time data for a control.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IDictionary" /> containing
		/// the design-time data for the <see cref="T:BarcodeXml" /> control.
		/// </returns>
		[SecurityPermission (SecurityAction.Demand, Unrestricted = true)]
		protected override IDictionary GetDesignModeState ()
		{
			IDictionary dictionary = new HybridDictionary ();
			dictionary["OriginalContent"] = ViewState["OriginalContent"];
			return dictionary;
		}

		/// <summary>
		/// Determines whether the server control contains any child controls.
		/// </summary>
		/// <returns>
		/// true if the control contains other controls; otherwise, false.
		/// </returns>
		[EditorBrowsable (EditorBrowsableState.Never)]
		public override bool HasControls ()
		{
			return base.HasControls ();
		}

		/// <summary>
		/// Renders the results to the output stream.
		/// </summary>
		protected override void Render (HtmlTextWriter output)
		{
			if (_xpathNavigator == null)
			{
				LoadXPathDocument ();
			}
			LoadTransformFromSource ();
			if (_xpathDocument != null || _xpathNavigator != null)
			{
				// No transform? Use identity...
				if (_transform == null)
				{
					_transform = _identityTransform;
				}

				// Ensure we have a navigator
				if (_xpathDocument != null)
				{
					_xpathNavigator = _xpathDocument.CreateNavigator ();
				}
				if (_xpathNavigator != null)
				{
					// Ensure image/barcode extension is registered
					RegisterExtensionObject ("http://schemas.siamzen.com/barcodes",
						new XsltBarcodeExtension ());

					// Now we can perform the transformation
					Transform.Transform (_xpathNavigator, _transformArgumentList, output);
				}
			}
		}
		#endregion

		#region Private Methods
		private void LoadTransformFromSource ()
		{
			if ((_transform == null) && (!string.IsNullOrEmpty (_transformSource) && 
				(_transformSource.Trim ().Length != 0)))
			{
				string physicalPath = base.MapPathSecure (_transformSource);
				string cacheKey = "p" + physicalPath;
				_transform = (XslCompiledTransform) HttpContext.Current.Cache.Get (cacheKey);
				if (_transform == null)
				{
					using (CacheDependency dependency = new CacheDependency (physicalPath))
					{
						Stream stream = new FileStream (physicalPath, FileMode.Open, FileAccess.Read);
						XmlTextReader xmlReader = new XmlTextReader (physicalPath, stream);
						_transform = new XslCompiledTransform ();
						_transform.Load (xmlReader);

						HttpContext.Current.Cache.Insert (cacheKey, _transform, dependency);
					}
				}
			}
		}

		private void LoadXPathDocument ()
		{
			if (!string.IsNullOrEmpty (_documentContent))
			{
				StringReader reader = new StringReader (_documentContent);
				_xpathDocument = new XPathDocument (reader);
			}
			else if (!string.IsNullOrEmpty (_documentSource))
			{
				string physicalPath = base.MapPathSecure (_documentSource);
				string cacheKey = "p" + physicalPath;
				_xpathDocument = (XPathDocument) HttpContext.Current.Cache.Get (cacheKey);
				if (_xpathDocument == null)
				{
					using (CacheDependency dependency = new CacheDependency (physicalPath))
					{
						Stream stream = new FileStream (physicalPath, FileMode.Open, FileAccess.Read);
						XmlTextReader xmlReader = new XmlTextReader (physicalPath, stream);
						_xpathDocument = new XPathDocument (xmlReader);

						HttpContext.Current.Cache.Insert (cacheKey, _xpathDocument, dependency);
					}
				}
			}
		}
		#endregion
	}
}
