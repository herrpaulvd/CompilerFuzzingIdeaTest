﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TestedCompiler {
    using System;
    
    
    /// <summary>
    ///   Класс ресурса со строгой типизацией для поиска локализованных строк и т.д.
    /// </summary>
    // Этот класс создан автоматически классом StronglyTypedResourceBuilder
    // с помощью такого средства, как ResGen или Visual Studio.
    // Чтобы добавить или удалить член, измените файл .ResX и снова запустите ResGen
    // с параметром /str или перестройте свой проект VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "17.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Возвращает кэшированный экземпляр ResourceManager, использованный этим классом.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("TestedCompiler.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Перезаписывает свойство CurrentUICulture текущего потока для всех
        ///   обращений к ресурсу с помощью этого класса ресурса со строгой типизацией.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на fun CheckAddOverflow
        ///param
        ///	long left
        ///	long right
        ///	long min
        ///	long max
        ///local
        ///	long temp
        ///	long flag
        ///do
        ///	lesseq right 0 flag
        ///	gotoif flag end1
        ///
        ///	sub max right temp
        ///	lesseq left temp flag
        ///	gotoif flag end1
        ///	
        ///	throw overflow
        ///	label end1
        ///
        ///	greatereq right 0 flag
        ///	gotoif flag end2
        ///
        ///	sub min right temp
        ///	greatereq left temp flag
        ///	gotoif flag end2
        ///
        ///	throw overflow
        ///	label end2
        ///
        ///	mov 48 left
        ///	ret
        ///end
        ///
        ///fun CheckSubOverflow
        ///param
        ///	long left
        ///	long right
        ///	long min
        ///	long max
        ///local
        ///	l [остаток строки не уместился]&quot;;.
        /// </summary>
        internal static string ExtraFunctions {
            get {
                return ResourceManager.GetString("ExtraFunctions", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Ищет локализованную строку, похожую на @get-current(@name) =
        ///	name(@name) &amp; type(var);
        ///
        ///@get-local(@name) =
        ///	@get-current(@name)
        ///	?| relation(parent) . @get-local(@name)
        ///	;
        ///
        ///.
        /// </summary>
        internal static string SearchScript {
            get {
                return ResourceManager.GetString("SearchScript", resourceCulture);
            }
        }
    }
}