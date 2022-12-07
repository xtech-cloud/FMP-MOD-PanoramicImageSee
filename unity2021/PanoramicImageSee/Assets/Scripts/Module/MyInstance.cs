

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using LibMVCS = XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.PanoramicImageSee.LIB.Proto;
using XTC.FMP.MOD.PanoramicImageSee.LIB.MVCS;
using System.IO;

namespace XTC.FMP.MOD.PanoramicImageSee.LIB.Unity
{
    /// <summary>
    /// 实例类
    /// </summary>
    public class MyInstance : MyInstanceBase
    {
        public class UiReference
        {
            public RawImage background;
            public RawImage renderer;
            public RawImage pending;
        }

        public class WorldReference
        {
            public Camera camera;
            public MeshRenderer sphere;
        }

        private UiReference uiReference_ = new UiReference();
        private WorldReference worldReference_ = new WorldReference();
        private ContentReader contentReader_ = null;

        private Material cloneSkyboxMaterial_;
        private Material originSkyboxMaterial_;

        public MyInstance(string _uid, string _style, MyConfig _config, MyCatalog _catalog, LibMVCS.Logger _logger, Dictionary<string, LibMVCS.Any> _settings, MyEntryBase _entry, MonoBehaviour _mono, GameObject _rootAttachments)
            : base(_uid, _style, _config, _catalog, _logger, _settings, _entry, _mono, _rootAttachments)
        {
        }

        /// <summary>
        /// 当被创建时
        /// </summary>
        /// <remarks>
        /// 可用于加载主题目录的数据
        /// </remarks>
        public void HandleCreated()
        {
            uiReference_.background = rootUI.transform.Find("bg").GetComponent<RawImage>();
            uiReference_.renderer = rootUI.transform.Find("renderer").GetComponent<RawImage>();
            uiReference_.pending = rootUI.transform.Find("pending").GetComponent<RawImage>();
            worldReference_.camera = rootWorld.transform.Find("camera").GetComponent<Camera>();
            worldReference_.camera.gameObject.SetActive(false);
            worldReference_.sphere = rootWorld.transform.Find("sphere").GetComponent<MeshRenderer>();

            int renderTextureWidth = (int)uiReference_.renderer.rectTransform.rect.width;
            int renderTextureHeight = (int)uiReference_.renderer.rectTransform.rect.height;
            var renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 16, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            uiReference_.renderer.texture = renderTexture;
            worldReference_.camera.targetTexture = renderTexture;

            applyStyle();
            bindEvents();
        }

        /// <summary>
        /// 当被删除时
        /// </summary>
        public void HandleDeleted()
        {
        }

        /// <summary>
        /// 当被打开时
        /// </summary>
        /// <remarks>
        /// 可用于加载内容目录的数据
        /// </remarks>
        public void HandleOpened(string _source, string _uri)
        {
            rootWorld.transform.localPosition = new Vector3(
            style_.spaceGrid.position.x,
            style_.spaceGrid.position.y,
            style_.spaceGrid.position.z
            );

            worldReference_.camera.gameObject.SetActive(true);
            wrapGestureCamera(worldReference_.camera.transform);

            if (null == cloneSkyboxMaterial_)
            {
                originSkyboxMaterial_ = RenderSettings.skybox;
                var material = rootAttachments.transform.Find("SkyboxRenderer").GetComponent<MeshRenderer>().material;
                cloneSkyboxMaterial_ = GameObject.Instantiate(material);
                RenderSettings.skybox = cloneSkyboxMaterial_;
            }

            contentReader_ = new ContentReader(contentObjectsPool);
            contentReader_.AssetRootPath = settings_["path.assets"].AsString();
            uiReference_.pending.gameObject.SetActive(true);
            uiReference_.renderer.gameObject.SetActive(false);
            //uiReference_.tfToolBar.gameObject.SetActive(false);
            loadImage(_source, _uri);


            rootUI.gameObject.SetActive(true);
            rootWorld.gameObject.SetActive(true);
        }

        /// <summary>
        /// 当被关闭时
        /// </summary>
        public void HandleClosed()
        {
            rootUI.gameObject.SetActive(false);
            rootWorld.gameObject.SetActive(false);

            if (null != cloneSkyboxMaterial_)
            {
                GameObject.Destroy(cloneSkyboxMaterial_);
                cloneSkyboxMaterial_ = null;
            }
        }

        /// <summary>
        /// 为摄像机添加手势操作
        /// </summary>
        /// <param name="_camera"></param>
        private void wrapGestureCamera(Transform _camera)
        {
            var camera = _camera.GetComponent<Camera>();
            // 水平滑动
            var swipeH = uiReference_.renderer.gameObject.AddComponent<HedgehogTeam.EasyTouch.QuickSwipe>();
            swipeH.swipeDirection = HedgehogTeam.EasyTouch.QuickSwipe.SwipeDirection.Horizontal;
            swipeH.onSwipeAction = new HedgehogTeam.EasyTouch.QuickSwipe.OnSwipeAction();
            swipeH.onSwipeAction.AddListener((_gesture) =>
            {
                // 忽略摄像机视窗外
                if (_gesture.position.x < camera.pixelRect.x ||
                _gesture.position.x > camera.pixelRect.x + camera.pixelRect.width ||
                _gesture.position.y < camera.pixelRect.y ||
                _gesture.position.y > camera.pixelRect.y + camera.pixelRect.height)
                    return;
                var vec = _camera.localRotation.eulerAngles;
                vec.y = vec.y + _gesture.swipeVector.x;
                _camera.localRotation = Quaternion.Euler(vec.x, vec.y, vec.z);
            });
            // 垂直滑动
            var swipeV = uiReference_.renderer.gameObject.AddComponent<HedgehogTeam.EasyTouch.QuickSwipe>();
            swipeV.swipeDirection = HedgehogTeam.EasyTouch.QuickSwipe.SwipeDirection.Vertical;
            swipeV.onSwipeAction = new HedgehogTeam.EasyTouch.QuickSwipe.OnSwipeAction();
            swipeV.onSwipeAction.AddListener((_gesture) =>
            {
                // 忽略摄像机视窗外
                if (_gesture.position.x < camera.pixelRect.x ||
                _gesture.position.x > camera.pixelRect.x + camera.pixelRect.width ||
                _gesture.position.y < camera.pixelRect.y ||
                _gesture.position.y > camera.pixelRect.y + camera.pixelRect.height)
                    return;
                var vec = _camera.localRotation.eulerAngles;
                vec.x = vec.x - _gesture.swipeVector.y;
                // 限制仰俯角
                if (vec.x > 70 && vec.x < 180)
                    vec.x = 70;
                if (vec.x < 290 && vec.x > 180)
                    vec.x = 290;
                _camera.rotation = Quaternion.Euler(vec.x, vec.y, vec.z);
            });
            // 捏合
            /*
            var pinch = _camera.gameObject.AddComponent<HedgehogTeam.EasyTouch.QuickPinch>();
            pinch.onPinchAction.AddListener((_gesture) =>
            {
                // 忽略摄像机视窗外
                if (_gesture.position.x < camera.pixelRect.x ||
                _gesture.position.x > camera.pixelRect.x + camera.pixelRect.width ||
                _gesture.position.y < camera.pixelRect.y ||
                _gesture.position.y > camera.pixelRect.y + camera.pixelRect.height)
                    return;
                _camera.GetComponent<Camera>().fieldOfView *= _gesture.deltaPinch;
            });
            */
        }

        private void loadImage(string _source, string _uri)
        {
            string contentUri = Path.GetDirectoryName(_uri);
            contentReader_.ContentUri = "";
            contentReader_.LoadTexture(_uri, (_texture) =>
            {
                worldReference_.sphere.material.mainTexture = _texture;
                uiReference_.pending.gameObject.SetActive(false);
                uiReference_.renderer.gameObject.SetActive(true);
            }, () =>
            {

            });
        }

        private void applyStyle()
        {
            uiReference_.background.gameObject.SetActive(style_.background.visible);
            Color color;
            ColorUtility.TryParseHtmlString(style_.background.color, out color);
            uiReference_.background.color = color;

            if (!string.IsNullOrEmpty(style_.pending.image))
            {
                loadTextureFromTheme(style_.pending.image, (_texture) =>
                {
                    uiReference_.pending.texture = _texture;
                    uiReference_.pending.SetNativeSize();
                }, () =>
                {

                });
            }

            //alignByAncor(uiReference_.tfToolBar, style_.toolBar.anchor);
        }

        private void bindEvents()
        {
        }

    }
}
