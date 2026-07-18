// General Settings JS
function previewLogo(input){
    if(input.files&&input.files[0]){
        var reader=new FileReader();
        reader.onload=function(e){
            var img=document.getElementById('logoPreview');
            var icon=document.getElementById('logoIcon');
            img.src=e.target.result;img.style.display='block';icon.style.display='none';
        };
        reader.readAsDataURL(input.files[0]);
    }
}

function saveSettings(){
    var gymName=document.getElementById('gymName').value.trim();
    if(!gymName){toastr.error('Gym name is required');return;}
    toastr.success('Settings saved successfully');
}

document.addEventListener('DOMContentLoaded',function(){
    toastr.options={positionClass:'toast-top-right',timeOut:3000};
});

// -- Select2 init --
function initSelect2() {
    if (typeof $ !== 'undefined' && $.fn && $.fn.select2) {
        $('select.form-select, select.gp-select').select2({
            theme: 'default',
            width: '100%',
            dropdownParent: document.body
        });
    }
}
// Re-run on each modal open
document.addEventListener('show.bs.modal', function() { setTimeout(initSelect2, 50); });
document.addEventListener('DOMContentLoaded', initSelect2);
