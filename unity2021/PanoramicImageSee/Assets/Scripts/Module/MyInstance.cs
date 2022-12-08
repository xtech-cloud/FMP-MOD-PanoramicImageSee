

using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using LibMVCS = XTC.FMP.LIB.MVCS;
using XTC.FMP.MOD.PanoramicImageSee.LIB.Proto;
using XTC.FMP.MOD.PanoramicImageSee.LIB.MVCS;
using System.IO;
using SoftMasking;
using System;
using System.Collections;

namespace XTC.FMP.MOD.PanoramicImageSee.LIB.Unity
{
    /// <summary>
    /// 实例类
    /// </summary>
    public class MyInstance : MyInstanceBase
    {
        public class UiReference
        {
            public RectTransform container;
            public RawImage background;
            public RawImage renderer;
            public RawImage pending;
            public RectTransform toolbar;
            public Button btnClose;
            public Button btnZoomIn;
            public Button btnZoomOut;
            public Text txtFOV;
        }

        public class WorldReference
        {
            public Camera camera;
            public MeshRenderer sphere;
        }

        private UiReference uiReference_ = new UiReference();
        private WorldReference worldReference_ = new WorldReference();
        private ContentReader contentReader_ = null;

        private Dictionary<string, Func<IEnumerator>> openWithEffectHandlerS = new Dictionary<string, Func<IEnumerator>>();
        private Dictionary<string, Func<Action, IEnumerator>> closeWithEffectHandlerS = new Dictionary<string, Func<Action, IEnumerator>>();
        private Dictionary<string, Action> effectBuilderS = new Dictionary<string, Action>();

        private int[] fovS_ = new int[] { 30, 40, 50, 60, 80, 100, 120 };
        private int fovIndex_ = 3;

        public MyInstance(string _uid, string _style, MyConfig _config, MyCatalog _catalog, LibMVCS.Logger _logger, Dictionary<string, LibMVCS.Any> _settings, MyEntryBase _entry, MonoBehaviour _mono, GameObject _rootAttachments)
            : base(_uid, _style, _config, _catalog, _logger, _settings, _entry, _mono, _rootAttachments)
        {
            openWithEffectHandlerS["TeleporterZoomEffect"] = openWithTeleporterZoomEffect;
            closeWithEffectHandlerS["TeleporterZoomEffect"] = closeWithTeleporterZoomEffect;
            effectBuilderS["TeleporterZoomEffect"] = buildTeleporterZoomEffect;
        }

        /// <summary>
        /// 当被创建时
        /// </summary>
        /// <remarks>
        /// 可用于加载主题目录的数据
        /// </remarks>
        public void HandleCreated()
        {
            uiReference_.container = rootUI.transform.Find("container").GetComponent<RectTransform>();
            uiReference_.container.GetComponent<Image>().enabled = false;
            uiReference_.background = rootUI.transform.Find("container/bg").GetComponent<RawImage>();
            uiReference_.renderer = rootUI.transform.Find("container/renderer").GetComponent<RawImage>();
            uiReference_.pending = rootUI.transform.Find("pending").GetComponent<RawImage>();
            uiReference_.toolbar = rootUI.transform.Find("container/toolbar").GetComponent<RectTransform>();
            uiReference_.btnClose = rootUI.transform.Find("container/toolbar/btnClose").GetComponent<Button>();
            uiReference_.btnZoomIn = rootUI.transform.Find("container/toolbar/btnZoomIn").GetComponent<Button>();
            uiReference_.btnZoomOut = rootUI.transform.Find("container/toolbar/btnZoomOut").GetComponent<Button>();
            uiReference_.txtFOV = rootUI.transform.Find("container/toolbar/txtFOV").GetComponent<Text>();
            worldReference_.camera = rootWorld.transform.Find("camera").GetComponent<Camera>();
            worldReference_.camera.gameObject.SetActive(false);
            worldReference_.sphere = rootWorld.transform.Find("sphere").GetComponent<MeshRenderer>();

            int renderTextureWidth = (int)uiReference_.renderer.rectTransform.rect.width;
            int renderTextureHeight = (int)uiReference_.renderer.rectTransform.rect.height;
            var renderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 16, RenderTextureFormat.ARGB32);
            renderTexture.Create();
            uiReference_.renderer.texture = renderTexture;
            worldReference_.camera.targetTexture = renderTexture;

            wrapGesture();
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

            fovIndex_ = style_.fov - 1;
            updateFOV();

            worldReference_.camera.gameObject.SetActive(true);
            uiReference_.txtFOV.text = worldReference_.camera.fieldOfView.ToString();

            contentReader_ = new ContentReader(contentObjectsPool);
            contentReader_.AssetRootPath = settings_["path.assets"].AsString();
            uiReference_.pending.gameObject.SetActive(true);
            uiReference_.renderer.gameObject.SetActive(false);
            //uiReference_.tfToolBar.gameObject.SetActive(false);

            rootUI.gameObject.SetActive(true);
            rootWorld.gameObject.SetActive(false);

            // load image
            string contentUri = Path.GetDirectoryName(_uri);
            contentReader_.ContentUri = "";
            contentReader_.LoadTexture(_uri, (_texture) =>
            {
                worldReference_.sphere.material.mainTexture = _texture;
                uiReference_.pending.gameObject.SetActive(false);
                uiReference_.renderer.gameObject.SetActive(true);
                rootWorld.gameObject.SetActive(true);

                Func<IEnumerator> handler = null;
                if (openWithEffectHandlerS.TryGetValue(style_.effect.active, out handler))
                {
                    mono_.StartCoroutine(handler());
                }
            }, () =>
            {

            });
        }

        /// <summary>
        /// 当被关闭时
        /// </summary>
        public void HandleClosed()
        {
        }

        /// <summary>
        /// 当被关闭时
        /// </summary>
        public void AsyncHandleClosed(Action _onFinish)
        {
            Func<Action, IEnumerator> handler = null;
            if (closeWithEffectHandlerS.TryGetValue(style_.effect.active, out handler))
            {
                mono_.StartCoroutine(handler(_onFinish));
            }
            else
            {
                onClosed();
                _onFinish();
            }
        }


        private void onClosed()
        {
            rootUI.gameObject.SetActive(false);
            rootWorld.gameObject.SetActive(false);
            contentReader_ = null;
        }

        /// <summary>
        /// 为摄像机添加手势操作
        /// </summary>
        private void wrapGesture()
        {
            // 水平滑动
            var swipeH = uiReference_.renderer.gameObject.AddComponent<HedgehogTeam.EasyTouch.QuickSwipe>();
            swipeH.swipeDirection = HedgehogTeam.EasyTouch.QuickSwipe.SwipeDirection.Horizontal;
            swipeH.onSwipeAction = new HedgehogTeam.EasyTouch.QuickSwipe.OnSwipeAction();
            swipeH.onSwipeAction.AddListener((_gesture) =>
            {
                if (uiReference_.renderer.gameObject != _gesture.pickedUIElement)
                    return;
                // 忽略摄像机视窗外
                /*
                if (_gesture.position.x < camera.pixelRect.x ||
                _gesture.position.x > camera.pixelRect.x + camera.pixelRect.width ||
                _gesture.position.y < camera.pixelRect.y ||
                _gesture.position.y > camera.pixelRect.y + camera.pixelRect.height)
                    return;
                */
                var vec = worldReference_.camera.transform.localRotation.eulerAngles;
                vec.y = vec.y + _gesture.swipeVector.x;
                worldReference_.camera.transform.localRotation = Quaternion.Euler(vec.x, vec.y, vec.z);
            });
            // 垂直滑动
            var swipeV = uiReference_.renderer.gameObject.AddComponent<HedgehogTeam.EasyTouch.QuickSwipe>();
            swipeV.swipeDirection = HedgehogTeam.EasyTouch.QuickSwipe.SwipeDirection.Vertical;
            swipeV.onSwipeAction = new HedgehogTeam.EasyTouch.QuickSwipe.OnSwipeAction();
            swipeV.onSwipeAction.AddListener((_gesture) =>
            {
                if (uiReference_.renderer.gameObject != _gesture.pickedUIElement)
                    return;

                // 忽略摄像机视窗外
                /*
                if (_gesture.position.x < camera.pixelRect.x ||
                _gesture.position.x > camera.pixelRect.x + camera.pixelRect.width ||
                _gesture.position.y < camera.pixelRect.y ||
                _gesture.position.y > camera.pixelRect.y + camera.pixelRect.height)
                    return;
                */
                var vec = worldReference_.camera.transform.localRotation.eulerAngles;
                vec.x = vec.x - _gesture.swipeVector.y;
                // 限制仰俯角
                if (vec.x > 70 && vec.x < 180)
                    vec.x = 70;
                if (vec.x < 290 && vec.x > 180)
                    vec.x = 290;
                worldReference_.camera.transform.rotation = Quaternion.Euler(vec.x, vec.y, vec.z);
            });
            // 捏合
            var pinch = uiReference_.renderer.gameObject.AddComponent<HedgehogTeam.EasyTouch.QuickPinch>();
            pinch.onPinchAction = new HedgehogTeam.EasyTouch.QuickPinch.OnPinchAction();
            pinch.onPinchAction.AddListener((_gesture) =>
            {
                if (uiReference_.renderer.gameObject != _gesture.pickedUIElement)
                    return;
                /*
                // 忽略摄像机视窗外
                if (_gesture.position.x < camera.pixelRect.x ||
                _gesture.position.x > camera.pixelRect.x + camera.pixelRect.width ||
                _gesture.position.y < camera.pixelRect.y ||
                _gesture.position.y > camera.pixelRect.y + camera.pixelRect.height)
                    return;
                */
                float fov = worldReference_.camera.fieldOfView;
                fov *= _gesture.deltaPinch;
                if (fov > 120)
                    fov = 120;
                if (fov < 30)
                    fov = 30;
                worldReference_.camera.fieldOfView = fov;
                uiReference_.txtFOV.text = worldReference_.camera.fieldOfView.ToString();
            });
        }


        private void applyStyle()
        {
            uiReference_.background.gameObject.SetActive(style_.background.visible);

            Func<string, Color> convertColor = (_color) =>
            {
                Color color = Color.white;
                ColorUtility.TryParseHtmlString(style_.background.color, out color);
                return color;
            };
            uiReference_.background.color = convertColor(style_.background.color);
            uiReference_.toolbar.GetComponent<Image>().color = convertColor(style_.toolbar.color);

            Action<string, RawImage, bool> loadTheme = (_file, _rawImage, _setNativeSize) =>
            {
                if (!string.IsNullOrEmpty(_file))
                {
                    loadTextureFromTheme(_file, (_texture) =>
                    {
                        _rawImage.texture = _texture;
                        if (_setNativeSize)
                            _rawImage.SetNativeSize();
                    }, () =>
                    {
                    });
                }
            };
            loadTheme(style_.pending.image, uiReference_.pending, true);
            loadTheme(style_.toolbar.buttonClose.image, uiReference_.btnClose.GetComponent<RawImage>(), false);
            loadTheme(style_.toolbar.buttonZoomIn.image, uiReference_.btnZoomIn.GetComponent<RawImage>(), false);
            loadTheme(style_.toolbar.buttonZoomOut.image, uiReference_.btnZoomOut.GetComponent<RawImage>(), false);

            alignByAncor(uiReference_.toolbar, style_.toolbar.anchor);
            var gridLayout = uiReference_.toolbar.GetComponent<GridLayoutGroup>();
            gridLayout.padding = new RectOffset(style_.toolbar.padding.left, style_.toolbar.padding.right, style_.toolbar.padding.top, style_.toolbar.padding.bottom);
            gridLayout.cellSize = new Vector2(style_.toolbar.cellSize.width, style_.toolbar.cellSize.height);
            gridLayout.spacing = new Vector2(style_.toolbar.spacing.x, style_.toolbar.spacing.y);
            uiReference_.toolbar.gameObject.SetActive(style_.toolbar.visible);
            Action builder = null;
            if (effectBuilderS.TryGetValue(style_.effect.active, out builder))
            {
                builder();
            }
        }

        private void bindEvents()
        {
            uiReference_.btnClose.onClick.AddListener(() =>
            {
                // 需要发布消息才能执行完整的包含资源清理的关闭过程
                Dictionary<string, object> data = new Dictionary<string, object>();
                data["uid"] = this.uid;
                data["delay"] = 0f;
                entry_.getDummyModel().Publish(MySubjectBase.Close, data);
            });

            uiReference_.btnZoomIn.onClick.AddListener(() =>
            {
                fovIndex_ -= 1;
                updateFOV();
            });

            uiReference_.btnZoomOut.onClick.AddListener(() =>
            {
                fovIndex_ += 1;
                updateFOV();
            });

            uiReference_.txtFOV.GetComponent<Button>().onClick.AddListener(() =>
            {
                fovIndex_ = 3;
                updateFOV();
            });
        }

        private void buildTeleporterZoomEffect()
        {
            var rtRoot = rootUI.GetComponent<RectTransform>();
            var componentSoftMask = uiReference_.container.gameObject.AddComponent<SoftMask>();
            var refSoftMask = rootAttachments.transform.Find("SoftMask").GetComponent<SoftMask>();
            componentSoftMask.defaultUIShader = refSoftMask.defaultUIShader;
            componentSoftMask.defaultUIETC1Shader = refSoftMask.defaultUIETC1Shader;
            uiReference_.container.GetComponent<Image>().enabled = true;
            var rtRenderer = uiReference_.renderer.rectTransform;
            rtRenderer.anchorMin = new Vector2(0.5f, 0.5f);
            rtRenderer.anchorMax = new Vector2(0.5f, 0.5f);
            rtRenderer.anchoredPosition = new Vector2(0, 0);
            rtRenderer.sizeDelta = new Vector2(rtRoot.rect.width, rtRoot.rect.height);
            uiReference_.container.sizeDelta = new Vector2(-rtRoot.rect.width, -rtRoot.rect.height);
        }

        private IEnumerator openWithTeleporterZoomEffect()
        {
            uiReference_.toolbar.gameObject.SetActive(false);

            var rtRoot = rootUI.GetComponent<RectTransform>();
            var fromSizeDelta = new Vector2(-rtRoot.rect.width, -rtRoot.rect.height);
            var toSizeDelta = new Vector2(-rtRoot.rect.width + rtRoot.rect.width * style_.effect.teleporterZoomEffect.scale,
                -rtRoot.rect.height + rtRoot.rect.height * style_.effect.teleporterZoomEffect.scale);
            var duration = style_.effect.teleporterZoomEffect.duration;

            yield return new WaitForEndOfFrame();
            uiReference_.container.sizeDelta = fromSizeDelta;

            float timer = 0;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                yield return new WaitForEndOfFrame();
                uiReference_.container.sizeDelta = Vector2.Lerp(fromSizeDelta, toSizeDelta, timer / duration);
            }
            yield return new WaitForEndOfFrame();
            uiReference_.container.sizeDelta = toSizeDelta;
            uiReference_.toolbar.gameObject.SetActive(style_.toolbar.visible);
        }

        private IEnumerator closeWithTeleporterZoomEffect(Action _onFinish)
        {
            uiReference_.toolbar.gameObject.SetActive(false);

            var rtRoot = rootUI.GetComponent<RectTransform>();
            var fromSizeDelta = new Vector2(-rtRoot.rect.width + rtRoot.rect.width * style_.effect.teleporterZoomEffect.scale,
                -rtRoot.rect.height + rtRoot.rect.height * style_.effect.teleporterZoomEffect.scale);
            var toSizeDelta = new Vector2(-rtRoot.rect.width, -rtRoot.rect.height);
            var duration = style_.effect.teleporterZoomEffect.duration;

            yield return new WaitForEndOfFrame();
            uiReference_.container.sizeDelta = fromSizeDelta;

            float timer = 0;
            while (timer < duration)
            {
                timer += Time.deltaTime;
                yield return new WaitForEndOfFrame();
                uiReference_.container.sizeDelta = Vector2.Lerp(fromSizeDelta, toSizeDelta, timer / duration);
            }
            yield return new WaitForEndOfFrame();
            uiReference_.container.sizeDelta = toSizeDelta;
            onClosed();
            _onFinish();
        }

        private void updateFOV()
        {
            if (fovIndex_ > fovS_.Length - 1)
                fovIndex_ = fovS_.Length - 1;
            if (fovIndex_ < 0)
                fovIndex_ = 0;
            worldReference_.camera.fieldOfView = fovS_[fovIndex_];
            uiReference_.txtFOV.text = worldReference_.camera.fieldOfView.ToString();
        }
    }
}
