// Employment Types JS
var types = [
    {id:1, name:'Full-time', desc:'40 hours per week, full benefits and paid leave', count:6},
    {id:2, name:'Part-time', desc:'Less than 20 hours per week, limited benefits', count:3},
    {id:3, name:'Trainer', desc:'Personal and group fitness trainers', count:4},
    {id:4, name:'Intern', desc:'Temporary training position, 3-6 months', count:1},
    {id:5, name:'Contractor', desc:'External service provider, project-based', count:1}
];

var currentPage=1, perPage=10, searchQuery='', editingId=null;

function filtered() {
    var q=searchQuery.toLowerCase();
    return types.filter(t=>!q||t.name.toLowerCase().includes(q)||t.desc.toLowerCase().includes(q));
}

function renderTable() {
    var data=filtered(); var total=data.length;
    var pages=Math.ceil(total/perPage)||1;
    if(currentPage>pages)currentPage=1;
    var slice=data.slice((currentPage-1)*perPage,currentPage*perPage);
    document.getElementById('typesTableBody').innerHTML=slice.length?slice.map((t,i)=>`
        <tr>
            <td>${(currentPage-1)*perPage+i+1}</td>
            <td><span style="font-weight:700;color:#1e293b;">${t.name}</span></td>
            <td style="color:#64748b;">${t.desc}</td>
            <td><span class="badge" style="background:#ccfbf1;color:#0f766e;border-radius:8px;font-size:.78rem;">${t.count} employees</span></td>
            <td><div style="display:flex;gap:6px;">
                <button class="gp-btn-icon gp-btn-edit" onclick="openModal(${t.id})"><i class="ri-pencil-line"></i></button>
                <button class="gp-btn-icon gp-btn-delete" onclick="deleteType(${t.id})"><i class="ri-delete-bin-line"></i></button>
            </div></td>
        </tr>`).join(''):'<tr><td colspan="5" class="text-center py-4 text-muted">No types found</td></tr>';
    document.getElementById('pageInfo').textContent=`Showing ${slice.length} of ${total}`;
    var btns=document.getElementById('pageBtns');btns.innerHTML='';
    for(var p=1;p<=pages;p++){var btn=document.createElement('button');btn.className='gp-page-btn'+(p===currentPage?' active':'');btn.textContent=p;btn.onclick=(function(pp){return function(){currentPage=pp;renderTable();};})(p);btns.appendChild(btn);}
}

function openModal(id) {
    editingId=id||null;
    document.getElementById('modalTitle').textContent=id?'Edit Type':'Add Employment Type';
    if(id){var t=types.find(x=>x.id===id);document.getElementById('fName').value=t.name;document.getElementById('fDesc').value=t.desc;}
    else{document.getElementById('fName').value='';document.getElementById('fDesc').value='';}
    new bootstrap.Modal(document.getElementById('typeModal')).show();
}

function saveType() {
    var name=document.getElementById('fName').value.trim();
    if(!name){toastr.error('Name is required');return;}
    if(editingId){var t=types.find(x=>x.id===editingId);t.name=name;t.desc=document.getElementById('fDesc').value;toastr.success('Updated');}
    else{types.push({id:Date.now(),name,desc:document.getElementById('fDesc').value,count:0});toastr.success('Added');}
    bootstrap.Modal.getInstance(document.getElementById('typeModal')).hide();
    renderTable();
}

function deleteType(id) {
    if(!confirm('Delete this type?'))return;
    types=types.filter(t=>t.id!==id);toastr.success('Deleted');renderTable();
}

function handleSearch(v){searchQuery=v;currentPage=1;renderTable();}
document.addEventListener('DOMContentLoaded',renderTable);

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
