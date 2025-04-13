<script>
	import '../App.css';
	import { ModeWatcher } from 'mode-watcher';
	import toast, { Toaster } from 'svelte-french-toast';
</script>

<ModeWatcher />
<Toaster position="top-center" />

<slot></slot>
