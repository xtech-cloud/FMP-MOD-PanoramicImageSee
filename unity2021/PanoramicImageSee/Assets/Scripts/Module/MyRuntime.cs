
using System.Collections.Generic;
using UnityEngine;
using LibMVCS = XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.PanoramicImageSee.LIB.MVCS;
using System.Collections;

namespace XTC.FMP.MOD.PanoramicImageSee.LIB.Unity
{
    /// <summary>
    /// 运行时类
    /// </summary>
    ///<remarks>
    /// 存储模块运行时创建的对象
    ///</remarks>
    public class MyRuntime : MyRuntimeBase
    {
        public MyRuntime(MonoBehaviour _mono, MyConfig _config, MyCatalog _catalog, Dictionary<string, LibMVCS.Any> _settings, LibMVCS.Logger _logger, MyEntryBase _entry)
            : base(_mono, _config, _catalog, _settings, _logger, _entry)
        {
        }

        /// <summary>
        /// 关闭实例
        /// </summary>
        /// <param name="_uid">实例的uid</param>
        /// <param name="_delay">延时时间，单位秒</param>
        public override void CloseInstanceAsync(string _uid, float _delay)
        {
            mono_.StartCoroutine(overrideCloseInstanceAsync(_uid, _delay));
        }

        private IEnumerator overrideCloseInstanceAsync(string _uid, float _delay)
        {
            logger_.Debug("close instance of {0}, uid is {1}", MyEntryBase.ModuleName, _uid);
            MyInstance instance;
            if (!instances.TryGetValue(_uid, out instance))
            {
                logger_.Error("instance not found");
                yield break;
            }
            yield return new WaitForSeconds(_delay);
            instance.AsyncHandleClosed(() =>
            {
                instance.contentObjectsPool.Dispose();
            });
        }


    }
}

