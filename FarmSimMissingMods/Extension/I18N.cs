using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows;
using System.Windows.Markup;
using FarmSimMapBuilder.Model.I18N;
using FarmSimMissingMods.Model.I18N;

namespace FarmSimMissingMods.Extension;

[MarkupExtensionReturnType(typeof(object))]
public class I18N : MarkupExtension, ICultureChangedEventListener
{
    
    #region Fields

    private readonly string m_Key;
    private FrameworkElement m_TargetObject;
    private PropertyInfo m_TargetProperty;
    
    private static readonly List<WeakReference> WeakReferences = new List<WeakReference>();
    
    #endregion

    #region Constructors

    public I18N(string key)
    {
        m_Key = key;

        I18NManager.CultureInfoChanged += OnCultureInfoChanged;
    }

    ~I18N()
    {
        I18NManager.CultureInfoChanged -= OnCultureInfoChanged;
    }

    #endregion

    #region Methods
    
    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        if (string.IsNullOrEmpty(m_Key))
        {
            throw new ArgumentException("You must set the I18N Key");
        }
        
        var providerValueTarget = (IProvideValueTarget) serviceProvider.GetService(typeof(IProvideValueTarget));

        m_TargetObject = (FrameworkElement) providerValueTarget?.TargetObject;
        if (m_TargetObject != null)
        {
            m_TargetObject.Unloaded += OnTargetObjectUnloaded;

            if (providerValueTarget?.TargetProperty is DependencyProperty dependencyProperty)
            {
                m_TargetProperty = m_TargetObject.GetType().GetProperty(dependencyProperty.Name);
            }
        }

        AddWeakReference(this);

        return I18NManager.GetTranslation(m_Key);
    }

    private void OnCultureInfoChanged(object sender, EventArgs eventArgs)
    {
        OnCultureChanged();
    }
    
    public void OnCultureChanged()
    {
        var newValue = I18NManager.GetTranslation(m_Key);
        m_TargetProperty.SetValue(m_TargetObject, newValue, null);
    }
    
    void OnTargetObjectUnloaded(object sender, RoutedEventArgs e)
    {
        m_TargetObject.Unloaded -= OnTargetObjectUnloaded;
        RemoveListener(this);
    }

    private static void AddWeakReference(ICultureChangedEventListener listener)
    {
        var wr = new WeakReference(listener, true);
        WeakReferences.Add(wr);
    }
    
    private static void RemoveListener(ICultureChangedEventListener listener)
    {
        foreach (var it in WeakReferences.ToArray())
        {
            if (it.Target == listener)
            {
                WeakReferences.Remove(it);
            }
        }
    }

    public static void OnCultureUpdated()
    {
        foreach (var weakReference in WeakReferences.ToArray())
        {
            var listener = (ICultureChangedEventListener) weakReference.Target;
            
            if (listener == null)
            {
                WeakReferences.Remove(weakReference);
            }
            else
            {
                listener.OnCultureChanged();
            }
        }
    }

    #endregion
}