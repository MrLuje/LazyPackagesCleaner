using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace MrLuje.LazyPackagesCleaner.Business.Commands
{
    public abstract class BaseCommand
    {
        protected bool ShowAnimation { get; set; }

        protected string AnimationStartText { get; set; }

        protected string AnimationEndText { get; set; }

        protected object Icon;
        uint statusBarCookie = 1;

        int frozenState;

        private IVsStatusbar bar;
        protected IVsStatusbar StatusBar
        {
            get
            {
                return bar ?? (bar = Package.GetGlobalService(typeof(SVsStatusbar)) as IVsStatusbar);
            }
        }

        protected BaseCommand(bool showAnimations)
        {
            this.ShowAnimation = showAnimations;
        }

        protected virtual void AnimationProgress(uint current, uint total)
        {
            if (frozenState == 0)
                StatusBar.Progress(ref statusBarCookie, 1, "", current, total);
        }

        protected virtual void StartAnimation()
        {
            StatusBar.IsFrozen(out frozenState);

            if (frozenState == 0)
            {
                StatusBar.SetText(AnimationStartText);

                StatusBar.Animation(1, ref Icon);
            }   
        }

        protected void SetStatusBarText(string text)
        {
            StatusBar.IsFrozen(out frozenState);

            if (frozenState == 0)
            {
                StatusBar.SetText(text);   
            }
        }

        protected virtual void EndAnimation()
        {
            StatusBar.IsFrozen(out frozenState);

            if (frozenState == 0)
            {
                // Clear the progress bar.
                StatusBar.Progress(ref statusBarCookie, 0, "", 0, 0);
                StatusBar.Animation(0, ref Icon);
                StatusBar.SetText(AnimationEndText);
            }
        }

        public void Execute()
        {
            StartAnimation();
            ExecuteCommand();
            EndAnimation();
        }

        protected abstract void ExecuteCommand();
    }
}
