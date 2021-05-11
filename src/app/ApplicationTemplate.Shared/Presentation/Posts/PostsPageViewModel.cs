using System;
using System.Collections.Immutable;
using System.Threading;
using System.Threading.Tasks;
using ApplicationTemplate.Business;
using ApplicationTemplate.Client;
using Chinook.DataLoader;
using Chinook.DynamicMvvm;
using Chinook.StackNavigation;

namespace ApplicationTemplate.Presentation
{
    public partial class PostsPageViewModel : ViewModel
    {
        private readonly Func<Task> _onGetPostsCalled;

        public PostsPageViewModel(Func<Task> onGetPostsCalled = null)
        {
            _onGetPostsCalled = onGetPostsCalled;
        }

        public IDynamicCommand NavigateToNewPost => this.GetCommandFromTask(async ct =>
        {
            await this.GetService<IStackNavigator>().Navigate(ct, () => new EditPostPageViewModel());
        });

        public IDynamicCommand NavigateToPost => this.GetCommandFromTask<PostData>(async (ct, post) =>
        {
            await this.GetService<IStackNavigator>().Navigate(ct, () => new EditPostPageViewModel(post));
        });

        public IDynamicCommand RefreshPosts => this.GetCommandFromDataLoaderRefresh(Posts);

        public IDataLoader Posts => this.GetDataLoader(GetPosts);

        private async Task<ImmutableList<PostData>> GetPosts(CancellationToken ct)
        {
            if (_onGetPostsCalled != null)
            {
                await _onGetPostsCalled();
            }

            return await this.GetService<IPostService>().GetPosts(ct);
        }
    }
}
