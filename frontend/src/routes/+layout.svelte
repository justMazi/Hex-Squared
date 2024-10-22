<script>
	import '../app.css';
	import { ModeWatcher } from 'mode-watcher';
	import { Toaster } from '$lib/components/ui/sonner';
</script>

<div class="flex">
	<ModeWatcher />
	<Toaster expand={true} position="top-center" />

	<slot></slot>
</div>
